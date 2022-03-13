
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VoteData : UdonSharpBehaviour
{
    public RatherGameController controller;

    public Text timeoutText;
    public GameObject[] scoreboardBox;
    public Text[] scoreboardText;

    [UdonSynced] private bool voting = false;
    [UdonSynced] private byte[] votes;
    [UdonSynced] private bool[] voted;

    [UdonSynced] public bool timed = true;
    [UdonSynced] private int timeout = 10;
    [UdonSynced] private long firstVoteTick = 0;

    private int[] localVoteScoreboard;
    private int[] localVoteScoreboardOffsets;

    private void Start()
    {
        votes = new byte[controller.maxPlayerCount];
        voted = new bool[controller.maxPlayerCount];

        localVoteScoreboard = new int[controller.maxPlayerCount];
        localVoteScoreboardOffsets = new int[controller.maxPlayerCount];
    }

    private void Update()
    {
        if (voting)
        {
            _UpdateLocalVote();

            if (timed && firstVoteTick != 0)
            {
                double timeLeft = timeout - (DateTime.UtcNow - new DateTime(firstVoteTick, DateTimeKind.Utc)).TotalSeconds;
                if (timeLeft > 0)
                {
                    timeoutText.gameObject.SetActive(true);
                    timeoutText.text = string.Format("{0:0.0}", timeLeft);
                }
                else
                {
                    timeoutText.gameObject.SetActive(false);

                    if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
                    {
                        Networking.SetOwner(Networking.LocalPlayer, gameObject);
                        firstVoteTick = 0;
                        RequestSerialization();
                        OnDeserialization();

                        controller._DoneVoting();
                    }
                }
            }
            else
            {
                timeoutText.gameObject.SetActive(false);
            }
        }
        else
        {
            timeoutText.gameObject.SetActive(false);
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        int playerId = controller.GetOwnedOrLowerReleasedIndex(player);
        localVoteScoreboard[playerId] = 0;
        localVoteScoreboardOffsets[playerId] = localVoteScoreboard[controller.GetOwnedOrLowerReleasedIndex(Networking.LocalPlayer)];

        SendCustomEventDelayedFrames("_UpdateScoreboard", 1);
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        int playerId = controller.GetOwnedOrLowerReleasedIndex(player);

        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        for (int i = playerId+1; i < controller.maxPlayerCount; i++)
        {
            if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
            {
                votes[i - 1] = votes[i];
                voted[i - 1] = voted[i];
            }

            localVoteScoreboard[i - 1] = localVoteScoreboard[i];
            localVoteScoreboardOffsets[i - 1] = localVoteScoreboardOffsets[i];
        }

        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            RequestSerialization();
            OnDeserialization();
        }
        SendCustomEventDelayedFrames("_UpdateScoreboard", 1);
    }

    public void _CountScoreboard()
    {
        byte vote = GetLocalVote();
        if (vote != 0)
        {
            for (int i = 0; i < controller.maxPlayerCount; i++)
            {
                if (votes[i] == vote)
                {
                    localVoteScoreboard[i] += 1;
                }
                else if (votes[i] == 0)
                {
                    localVoteScoreboardOffsets[i] += 1;
                }
            }
        }

        _UpdateScoreboard();
    }

    public void _UpdateScoreboard()
    {
        int playerId = controller.GetOwnedOrLowerReleasedIndex(Networking.LocalPlayer);

        VRCPlayerApi[] players = controller.GetSortedPlayerArray(controller.maxPlayerCount, null);
        for (int i = 0; i < players.Length; i++)
        {
            VRCPlayerApi player = players[i];
            scoreboardBox[i].SetActive(i != playerId);
            int percent = (localVoteScoreboard[playerId] - localVoteScoreboardOffsets[i]) > 0 ? Mathf.FloorToInt(((float) localVoteScoreboard[i] / (localVoteScoreboard[playerId] - localVoteScoreboardOffsets[i])) * 100f) : 0;
            scoreboardText[i].text = player.displayName + " " + percent + "% (" + localVoteScoreboard[i] + "/" + (localVoteScoreboard[playerId] - localVoteScoreboardOffsets[i]) + ")";
        }
        for (int i = players.Length; i < controller.maxPlayerCount; i++)
        {
            scoreboardBox[i].SetActive(false);
            scoreboardText[i].text = null;
        }
    }

    public byte GetLocalVote()
    {
        int playerId = controller.GetOwnedOrLowerReleasedIndex(Networking.LocalPlayer);
        return votes[playerId];
    }

    public void _ToggleTimed()
    {
        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            timed = !timed;
            RequestSerialization();
            OnDeserialization();
        }
    }

    public void _VoteYes()
    {
        if (voting) _Vote(true);
    }

    public void _VoteNo()
    {
        if (voting) _Vote(false);
    }

    private void _Vote(bool yes)
    {
        int playerId = controller.GetOwnedOrLowerReleasedIndex(Networking.LocalPlayer);
        byte currentVote = votes[playerId];
        byte newVote = (byte)(yes ? 1 : 2);

        if (currentVote != newVote)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            votes[playerId] = newVote;
            voted[playerId] = true;
            RequestSerialization();
            OnDeserialization();

            if (firstVoteTick == 0) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartCountdown");
        }

        _UpdateLocalVote();
    }

    public void StartCountdown()
    {
        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            if (firstVoteTick == 0)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                firstVoteTick = DateTime.UtcNow.Ticks;
                RequestSerialization();
                OnDeserialization();
            }
        }
    }

    private void _UpdateLocalVote()
    {
        byte vote = GetLocalVote();

        if (vote == 0 || voting == false)
        {
            controller.targetYesColor = controller.targetNoColor = controller.defaultImageColor;
            controller.targetDisplay1Color = controller.targetDisplay2Color = Color.white;
        }
        else if (vote == 1)
        {
            controller.targetNoColor = controller.defaultImageColor;
            controller.targetYesColor = new Color(0, 0.5f, 0, 0.95f);
            controller.targetDisplay1Color = Color.white;
            controller.targetDisplay2Color = new Color(1, 1, 1, 0.1f);
        }
        else if (vote == 2)
        {
            controller.targetYesColor = controller.defaultImageColor;
            controller.targetNoColor = new Color(0.5f, 0, 0, 0.95f);
            controller.targetDisplay2Color = Color.white;
            controller.targetDisplay1Color = new Color(1, 1, 1, 0.1f);
        }
    }

    public void SetVoting(bool vote)
    {
        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            voting = vote;
            RequestSerialization();
            OnDeserialization();
        }
    }

    public byte[] GetVotes()
    {
        return votes;
    }

    public bool AllVoted()
    {
        int totalVoted = 0;
        foreach (bool vote in voted)
        {
            if (vote) totalVoted++;
        }
        return totalVoted == controller.GetCurrentPlayerCount(controller.maxPlayerCount, null);
    }

    public void _ResetVotes()
    {
        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            voted = new bool[controller.maxPlayerCount];
            votes = new byte[controller.maxPlayerCount];
            firstVoteTick = 0;
            RequestSerialization();
            OnDeserialization();
        }
    }

    public void CheckVoting()
    {
        controller._VotedChanged();

        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            if (voting && AllVoted())
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                firstVoteTick = 0;
                RequestSerialization();

                controller._DoneVoting();
            }
        }
    }

    public override void OnDeserialization()
    {
        CheckVoting();
    }
}
