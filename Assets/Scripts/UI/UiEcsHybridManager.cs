using Systems;
using Data;
using Sounds;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UI
{
    public class UiEcsHybridManager : MonoBehaviour
    {
        public GameObject pausedPanel;
        public GameObject deadPanel;
        public GameObject menuPanel;
        public PostProcessVolume postProcessVolume;

        private CollisionsSystem _collisionsSystem;
        private Entity _gameDataEntity;
        private Entity _playerDataEntity;
        private EntityManager _entityManager;
        private DepthOfField _depthOfField;
        private SoundManager _soundManager;
        private GameState _state;

        private void Awake()
        {
            postProcessVolume.profile.TryGetSettings(out _depthOfField);
            _soundManager = FindObjectOfType<SoundManager>();
            _state = GameState.Menu;
        }

        private void LateUpdate()
        {
            if (_gameDataEntity == Entity.Null || _playerDataEntity == Entity.Null)
                _InitializeEntities();

            var healthData = _entityManager.GetComponentData<HealthComponent>(_playerDataEntity);

            if (_state != GameState.Menu && _state != GameState.Dead && Input.GetKeyDown(KeyCode.Escape))
                _SwitchState(_state == GameState.Paused ? GameState.Play : GameState.Paused);

            if (_state != GameState.Menu && _state != GameState.Dead && healthData.Health <= 0)
                _SwitchState(GameState.Dead);

            if (_state == GameState.Menu)
                _SwitchState(GameState.Menu);
        }

        public void StartGame()
        {
            _SwitchState(GameState.Play);
        }

        public void GoMenu()
        {
            _ResetData();
            _SwitchState(GameState.Menu);
        }

        public void Retry()
        {
            _ResetData();
            _SwitchState(GameState.Play);
        }

        private void _SwitchState(GameState state)
        {
            _state = _soundManager.State = state;
            menuPanel.SetActive(false);
            pausedPanel.SetActive(false);
            deadPanel.SetActive(false);

            if (_state == GameState.Play)
            {
                _depthOfField.active = false;
                _ManageSystems(true);
                return;
            }

            menuPanel.SetActive(_state == GameState.Menu);
            pausedPanel.SetActive(_state == GameState.Paused);
            deadPanel.SetActive(_state == GameState.Dead);

            _depthOfField.active = true;

            _ManageSystems(false);
        }

        private void _ResetData()
        {
            var healthData = _entityManager.GetComponentData<HealthComponent>(_playerDataEntity);

            healthData.Health = 3;

            _entityManager.SetComponentData(_playerDataEntity, healthData);

            var gameData = _entityManager.GetComponentData<GameComponent>(_gameDataEntity);

            gameData.CurrentEnemies = 0;

            _entityManager.SetComponentData(_gameDataEntity, gameData);
        }

        private void _InitializeEntities()
        {
            _entityManager = World.Active.EntityManager;
            var entities = _entityManager.GetAllEntities();
            var gameSystem = World.Active.GetExistingSystem(typeof(GameSystem));
            var inputSystem = World.Active.GetExistingSystem(typeof(PlayerInputSystem));
            var gameSystemComponentData = gameSystem.GetComponentDataFromEntity<GameComponent>();
            var inputComponentData = inputSystem.GetComponentDataFromEntity<InputComponent>();

            foreach (var entity in entities)
            {
                if (gameSystemComponentData.HasComponent(entity))
                    _gameDataEntity = entity;

                if (inputComponentData.HasComponent(entity))
                    _playerDataEntity = entity;
            }

            entities.Dispose();
        }

        private void _ManageSystems(bool isEnabled)
        {
            foreach (var componentSystemBase in World.Active.Systems)
            {
                if (componentSystemBase is GameSystem)
                    continue;

                componentSystemBase.Enabled = isEnabled;
            }
        }
    }

    public enum GameState
    {
        Play,
        Menu,
        Dead,
        Paused
    }
}