
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class RatherGameController : UdonSharpBehaviour
{
    [HideInInspector] public readonly int maxPlayerCount = 16;
    public RatherData ratherData;
    public VoteData voteData;

    public GameObject playerList;
    public GameObject[] playersBox;
    public Text[] playersText;
    public GameObject options;
    public Image yesImage;
    public Image noImage;
    [HideInInspector] public Color defaultImageColor = new Color(0, 0, 0, 0.95f);

    // Info
    public Text timedText;
    public Text masterText;
    public Text roundText;

    // Gamemode 0
    public GameObject display0;
    public Text nameDisplay0;
    public RawImage textureDisplay0;
    public Text textDisplay0;

    // Gamemode 1
    public Text ratherDisplay;
    public GameObject display1;
    public Text nameDisplay1;
    public RawImage textureDisplay1;
    public Text textDisplay1;
    public GameObject display2;
    public Text nameDisplay2;
    public RawImage textureDisplay2;
    public Text textDisplay2;

    // State
    private int localGamemode = -1;
    [UdonSynced, HideInInspector] public int gamemode;
    private int localRound = 0;
    [UdonSynced] private int round = 0;
    [UdonSynced] private int selected = 0;
    [UdonSynced] private int selected2 = 0;
    [UdonSynced] private string master;

    private Vector3 startDisplaySize = new Vector3(0.02f, 0.02f, 0.02f);
    private Vector3 targetDisplaySize;
    private Vector3 startTextSize = new Vector3(0.1f, 0.1f, 0.1f);
    private Vector3 targetTextSize;
    private Color targetDisplay0Color = Color.white;
    [HideInInspector] public Color targetDisplay1Color = Color.white;
    [HideInInspector] public Color targetDisplay2Color = Color.white;
    [HideInInspector] public Color targetYesColor;
    [HideInInspector] public Color targetNoColor;
    private Vector3 startOptionsPos = new Vector3(0, 5, 14.9f);
    private Vector3 upOptionsPos = new Vector3(0, 9, 15.1f);
    private Vector3 targetOptionsPos;

    private Vector3 startPlayerListPos = new Vector3(5, 11, 15.1f);
    private Vector3 middlePlayerListPos = new Vector3(0, 8, 14.9f);
    private Vector3 targetPlayerListPos;
    private Vector3[] targetPlayerBoxPos;

    private void Start()
    {
        if (ratherData.promptMode == 0 && ratherData.texturePrompts.Length < 2)
        {
            Debug.LogError("Not enough texture prompts in choices configuration");
            return;
        }
        if (ratherData.promptMode == 1 && ratherData.textPrompts.Length < 2)
        {
            Debug.LogError("Not enough text prompts in choices configuration");
            return;
        }

        gamemode = ratherData.defaultGamemode;

        targetDisplaySize = startDisplaySize;
        targetTextSize = startTextSize;
        targetPlayerListPos = startPlayerListPos;
        targetOptionsPos = startOptionsPos;
        targetYesColor = targetNoColor = defaultImageColor;

        targetPlayerBoxPos = new Vector3[maxPlayerCount];
        _ResetPlayerBoxes();

        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            _ChangeGamemode(gamemode);
        }
    }

    private void Update()
    {
        float t = Time.deltaTime * 10;

        if (gamemode == 0)
        {
            display0.transform.localScale = Vector3.Lerp(display0.transform.localScale, targetDisplaySize, t);
            nameDisplay0.transform.localScale = Vector3.Lerp(nameDisplay0.transform.localScale, targetTextSize, t);
            textureDisplay0.color = textDisplay0.color = Color.Lerp(textureDisplay0.color, targetDisplay0Color, t);

            yesImage.color = Color.Lerp(yesImage.color, targetYesColor, t);
            noImage.color = Color.Lerp(noImage.color, targetNoColor, t);
        }
        else if (gamemode == 1)
        {
            ratherDisplay.transform.localScale = Vector3.Lerp(ratherDisplay.transform.localScale, targetTextSize, t);

            display1.transform.localScale = Vector3.Lerp(display1.transform.localScale, targetDisplaySize, t);
            nameDisplay1.transform.localScale = Vector3.Lerp(nameDisplay1.transform.localScale, targetTextSize, t);

            display2.transform.localScale = Vector3.Lerp(display2.transform.localScale, targetDisplaySize, t);
            nameDisplay2.transform.localScale = Vector3.Lerp(nameDisplay2.transform.localScale, targetTextSize, t);

            textureDisplay1.color = textDisplay1.color = Color.Lerp(textureDisplay1.color, targetDisplay1Color, t);
            textureDisplay2.color = textDisplay2.color = Color.Lerp(textureDisplay2.color, targetDisplay2Color, t);
        }

        playerList.transform.localPosition = Vector3.Lerp(playerList.transform.localPosition, targetPlayerListPos, t);
        options.transform.localPosition = Vector3.Lerp(options.transform.localPosition, targetOptionsPos, t);

        for (int i = 0; i < maxPlayerCount; i++)
        {
            playersBox[i].transform.localPosition = Vector2.Lerp(playersBox[i].transform.localPosition, targetPlayerBoxPos[i], t);
        }

        // State
        timedText.text = "Timed: " + (voteData.timed ? "On" : "Off");
        masterText.text = "Master: " + master;
        roundText.text = "Round: #" + round;

        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster && !master.Equals(Networking.LocalPlayer.displayName))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            master = Networking.LocalPlayer.displayName;
            RequestSerialization();
            OnDeserialization();
        }
    }

    // Control
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        _UpdatePlayerList(null);
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        _UpdatePlayerList(player);
        voteData.SendCustomEventDelayedFrames("CheckVoting", 1);
    }

    public override void OnDeserialization()
    {
        if (localRound != round)
        {
            localRound = round;
            _NewPrompt();
        }

        if (localGamemode != gamemode)
        {
            localGamemode = gamemode;
            GamemodeChanged();
        }
    }

    public void _GM0()
    {
        _ChangeGamemode(0);
    }
    public void _GM1()
    {
        _ChangeGamemode(1);
    }

    public void _ChangeGamemode(int mode)
    {
        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            localGamemode = gamemode = mode;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "GamemodeChanged");
            _RollNewPrompt();
        }
    }

    public void GamemodeChanged()
    {
        if (gamemode == 0)
        {
            display0.SetActive(true);
            ratherDisplay.gameObject.SetActive(false);
            display1.SetActive(false);
            display2.SetActive(false);
            yesImage.gameObject.SetActive(true);
            noImage.gameObject.SetActive(true);

            display0.transform.localScale = Vector3.zero;

            startDisplaySize = new Vector3(0.02f, 0.02f, 0.02f);
        }
        else if (gamemode == 1)
        {
            display0.SetActive(false);
            ratherDisplay.gameObject.SetActive(true);
            display1.SetActive(true);
            display2.SetActive(true);
            yesImage.gameObject.SetActive(false);
            noImage.gameObject.SetActive(false);

            display1.transform.localScale = Vector3.zero;
            display2.transform.localScale = Vector3.zero;

            startDisplaySize = Vector3.one;
        }
    }

    public void _VotedChanged()
    {
        byte[] votes = voteData.GetVotes();
        for (int i = 0; i < playersText.Length; i++)
        {
            Text t = playersText[i];
            t.color = votes[i] > 0 ? Color.white : Color.grey;
        }
    }

    public void _DoneVoting()
    {
        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            voteData.SetVoting(false);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "AllVoted");
        }
    }

    public void AllVoted()
    {
        targetPlayerListPos = middlePlayerListPos;
        targetOptionsPos = upOptionsPos;
        targetTextSize = Vector3.zero;
        targetYesColor = targetNoColor = defaultImageColor;
        targetDisplay0Color = targetDisplay1Color = targetDisplay2Color = Color.white;

        SendCustomEventDelayedSeconds("_ShowVotes", 2);
        SendCustomEventDelayedSeconds("_RollNewPrompt", 5);
    }

    public void _ResetVotes()
    {
        _ResetPlayerBoxes();
        targetPlayerListPos = startPlayerListPos;
        targetOptionsPos = startOptionsPos;

        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            voteData._ResetVotes();
        }
    }

    public void _RollNewPrompt()
    {
        _ResetVotes();

        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            int optionCount = ratherData.promptMode == 0 ? ratherData.texturePrompts.Length : ratherData.textPrompts.Length;
            selected = Random.Range(0, optionCount);
            do
            {
                selected2 = Random.Range(0, optionCount);
            }
            while (selected == selected2);

            round++;
            RequestSerialization();
            OnDeserialization();
        }
    }

    // UI
    public void _UpdatePlayerList(VRCPlayerApi excludePlayer)
    {
        VRCPlayerApi[] players = GetSortedPlayerArray(maxPlayerCount, excludePlayer);
        for (int i = 0; i < players.Length; i++)
        {
            VRCPlayerApi player = players[i];
            playersBox[i].SetActive(true);
            playersText[i].text = player.displayName;
        }
        for (int i = players.Length; i < maxPlayerCount; i++)
        {
            playersBox[i].SetActive(false);
            playersText[i].text = null;
        }
    }

    public void _ShowVotes()
    {
        voteData._CountScoreboard();

        byte[] votes = voteData.GetVotes();
        for (int i = 0; i < votes.Length; i++)
        {
            if (votes[i] == 0)
            {
                targetPlayerBoxPos[i] = new Vector3(0, playersBox[i].transform.localPosition.y, 0);
            }
            else if (votes[i] == 1)
            {
                targetPlayerBoxPos[i] = new Vector3(-225, playersBox[i].transform.localPosition.y, 0);
            }
            else if (votes[i] == 2)
            {
                targetPlayerBoxPos[i] = new Vector3(225, playersBox[i].transform.localPosition.y, 0);
            }
        }
    }

    private void _ResetPlayerBoxes()
    {
        for (int i = 0; i < maxPlayerCount; i++)
        {
            targetPlayerBoxPos[i] = new Vector3(0, playersBox[i].transform.localPosition.y, 0);
        }
    }

    public void _NewPrompt()
    {
        targetDisplaySize = Vector3.zero;
        SendCustomEventDelayedSeconds("_ShowNewPrompt", 1);
    }

    public void _ShowNewPrompt()
    {
        targetDisplaySize = startDisplaySize;

        targetTextSize = Vector3.zero;
        if (ratherData.promptMode == 0)
        {
            targetDisplay0Color = targetDisplay1Color = targetDisplay2Color = Color.black;
        }
        else if (ratherData.promptMode == 1)
        {
            targetDisplay0Color = targetDisplay1Color = targetDisplay2Color = Color.clear;
        }
        
        if (gamemode == 0)
        {
            nameDisplay0.transform.localScale = Vector3.zero;

            if (ratherData.promptMode == 0)
            {
                textureDisplay0.color = Color.black;
                nameDisplay0.text = ratherData.texturePrompts[selected].name;
                textureDisplay0.texture = ratherData.texturePrompts[selected];
            }
            else if (ratherData.promptMode == 1)
            {
                textureDisplay0.color = Color.clear;
                textDisplay0.text = ratherData.textPrompts[selected];
            }
        }
        else if (gamemode == 1)
        {
            nameDisplay1.transform.localScale = nameDisplay2.transform.localScale = Vector3.zero;

            if (ratherData.promptMode == 0)
            {
                textureDisplay1.color = textureDisplay2.color = Color.black;

                nameDisplay1.text = ratherData.texturePrompts[selected].name;
                textureDisplay1.texture = ratherData.texturePrompts[selected];

                nameDisplay2.text = ratherData.texturePrompts[selected2].name;
                textureDisplay2.texture = ratherData.texturePrompts[selected2];
            }
            else if (ratherData.promptMode == 1)
            {
                textureDisplay1.color = textureDisplay2.color = Color.clear;
                textDisplay1.text = ratherData.textPrompts[selected];
                textDisplay2.text = ratherData.textPrompts[selected2];
            }
        }

        SendCustomEventDelayedSeconds("_UnhideNewPrompt", 1);
    }

    public void _UnhideNewPrompt()
    {
        targetTextSize = startTextSize;
        targetDisplay0Color = targetDisplay1Color = targetDisplay2Color = Color.white;

        if (Networking.LocalPlayer != null && Networking.LocalPlayer.isMaster)
        {
            voteData.SetVoting(true);
        }
    }

    // Player helpers
    // Magical function to get each player their own index in an array (the actual index may change but the player will always have a unique index)
    public int GetOwnedOrLowerReleasedIndex(VRCPlayerApi player)
    {
        int occupiedIndexes = 0;
        for (int index = 1; player != null && index < player.playerId; index++)
        {
            if (VRCPlayerApi.GetPlayerById(index) != null)
            {
                occupiedIndexes++;
            }
        }
        return occupiedIndexes;
    }

    public int GetCurrentPlayerCount(int maxPlayers, VRCPlayerApi excludePlayer)
    {
        int playerCount = VRCPlayerApi.GetPlayerCount() - (excludePlayer != null ? 1 : 0);
        return maxPlayers <= 0 || playerCount < maxPlayers ? playerCount : maxPlayers;
    }

    public VRCPlayerApi[] GetSortedPlayerArray(int maxPlayers, VRCPlayerApi excludePlayer)
    {
        int playerCount = GetCurrentPlayerCount(maxPlayers, excludePlayer);
        VRCPlayerApi[] players = new VRCPlayerApi[playerCount];
        int occupiedIndexes = 0;
        for (int index = 1; occupiedIndexes < players.Length; index++)
        {
            VRCPlayerApi player;
            if ((player = VRCPlayerApi.GetPlayerById(index)) != null && player != excludePlayer)
            {
                players[occupiedIndexes++] = player;
            }
        }
        return players;
    }

    public bool ContainsPlayer(VRCPlayerApi[] array, VRCPlayerApi target)
    {
        foreach (VRCPlayerApi o in array) if (o == target) return true;
        return false;
    }

    public int GetPlayerIndex(VRCPlayerApi[] array, VRCPlayerApi target)
    {
        for (int i = 0; i < array.Length; i++) if (array[i] == target) return i;
        return -1;
    }
}
