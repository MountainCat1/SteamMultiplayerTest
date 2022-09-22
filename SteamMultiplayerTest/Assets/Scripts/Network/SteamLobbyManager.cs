using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.Transports;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Network
{
    public class SteamLobbyManager : MonoBehaviour
    {
        public static SteamLobbyManager Instance { get; private set; }

        // Public events
        // public Action OnLobbyEntered;

        // Steam Callbacks
        protected Callback<LobbyEnter_t> LobbyEnteredCallback;
        protected Callback<LobbyCreated_t> LobbyCreatedCallback;
        protected Callback<GameLobbyJoinRequested_t> JoinRequestCallback;

        // Variables
        public ulong CurrentLobbyID { get; set; }
        protected const string HostAddressKey = "HostAddress";
        protected const string LobbyNameKey = "name";
    
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private SteamNetworkingTransport networkTransport;
    
        // Configuration
        [SerializeField] private int maxPlayersInLobby = 4;

        #region Unity Methods

        private void OnEnable()
        {
            if (Instance != null)
                Debug.LogError("Singleton duplicated!");

            Instance = this;
        }

        private void Start()
        {
            SteamAPI.Init();
            if (!SteamAPI.IsSteamRunning())
            {
                Debug.LogError("Steam is not initialized");
                return;
            }

            // Register steam callbacks
            LobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            LobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        
            JoinRequestCallback = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        }

        #endregion
    
        #region Public Methods

        public void HostLobby()
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayersInLobby);
        }

        public void JoinLobby(CSteamID lobbyId)
        {
            SteamMatchmaking.JoinLobby(lobbyId);
        }

        #endregion

        #region SteamCallbackHandlers
        private void OnLobbyCreated(LobbyCreated_t callback)
        {
            if (callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError($"Failed to create a lobby: {callback.m_eResult}");
                return;
            }

            Debug.Log("Lobby created successfully");

            networkManager.StartHost();

            SteamMatchmaking.SetLobbyData(
                new CSteamID(callback.m_ulSteamIDLobby),
                HostAddressKey,
                SteamUser.GetSteamID().ToString());

            SteamMatchmaking.SetLobbyData(
                new CSteamID(callback.m_ulSteamIDLobby),
                LobbyNameKey,
                $"{SteamFriends.GetPersonaName()}'s lobby");
        }

        private void OnJoinRequest(GameLobbyJoinRequested_t callback)
        {
            Debug.Log("Request to join lobby");
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        private void OnLobbyEntered(LobbyEnter_t callback)
        {
            var lobbyID = new CSteamID(callback.m_ulSteamIDLobby);

            // Everyone
            CurrentLobbyID = callback.m_ulSteamIDLobby;

            // Clients
            if (networkManager.IsHost)
                return;

            networkTransport.ConnectToSteamID = callback.m_ulSteamIDLobby;

            networkManager.StartClient();
        }
    
        #endregion
    }
}