using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace _Source
{
    [Serializable]
    public class Weapon
    {
        public string id;
        public int damage;
        public float cooldown;
    }
    
    [Serializable]
    public class WeaponConfigList
    {
        public List<Weapon> weapons = new List<Weapon>();
    }

    public class RemoteConfigLoader : MonoBehaviour
    {
        [Header("Settings")]
        public string configUrl = "https://sa1shan.github.io/Configs/weapons.json";
        public float timeout = 5f;

        private string _localCachePath;
        
        private readonly List<Weapon> _defaultWeapons = new List<Weapon>
        {
            new Weapon { id = "default_sword", damage = 10, cooldown = 1.0f },
            new Weapon { id = "default_bow", damage = 5, cooldown = 0.5f }
        };

        void Start()
        {
            _localCachePath = Path.Combine(Application.persistentDataPath, "weapons_cache.json");
            StartCoroutine(FetchConfigRoutine());
        }

        private IEnumerator FetchConfigRoutine()
        {
            Debug.Log("[ConfigLoader] Начинаем загрузку конфига...");

            using (UnityWebRequest www = UnityWebRequest.Get(configUrl))
            {
                www.timeout = (int)timeout;
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ConfigLoader] Ошибка сети: {www.error}");
                    LoadLocalCacheOrDefaults();
                }
                else
                {
                    string json = www.downloadHandler.text;
                
                    // Пробуем распарсить и провалидировать
                    if (ProcessAndValidateConfig(json, out WeaponConfigList validConfig))
                    {
                        SaveLocalCache(json);
                        ApplyConfig(validConfig);
                    }
                    else
                    {
                        Debug.LogError("[ConfigLoader] Удаленный конфиг не прошел валидацию или поврежден.");
                        LoadLocalCacheOrDefaults();
                    }
                }
            }
        }
        
        private bool ProcessAndValidateConfig(string json, out WeaponConfigList validConfig)
        {
            validConfig = new WeaponConfigList();
            try
            {
                WeaponConfigList parsedData = JsonUtility.FromJson<WeaponConfigList>(json);
                if (parsedData == null || parsedData.weapons == null || parsedData.weapons.Count == 0)
                {
                    return false;
                }
                
                foreach (var w in parsedData.weapons)
                {
                    if (w.damage >= 0 && w.cooldown > 0)
                    {
                        validConfig.weapons.Add(w);
                    }
                    else
                    {
                        Debug.LogWarning($"[ConfigLoader] Валидация провалена для {w.id}. Damage: {w.damage}, Cooldown: {w.cooldown}. Оружие пропущено.");
                    }
                }
                
                return validConfig.weapons.Count > 0; 
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigLoader] Ошибка парсинга JSON: {e.Message}");
                return false;
            }
        }
        
        private void LoadLocalCacheOrDefaults()
        {
        
        
            if (File.Exists(_localCachePath))
            {
                Debug.Log("[ConfigLoader] Попытка загрузить локальный кеш...");
                string cachedJson = File.ReadAllText(_localCachePath);

                if (ProcessAndValidateConfig(cachedJson, out WeaponConfigList cachedConfig))
                {
                    Debug.Log("[ConfigLoader] Локальный кеш успешно загружен и применен.");
                    ApplyConfig(cachedConfig);
                    return;
                }
                else
                {
                    Debug.LogError("[ConfigLoader] Локальный кеш поврежден.");
                }
            }
            else
            {
                Debug.LogWarning("[ConfigLoader] Локальный кеш отсутствует.");
            }
            
            Debug.LogWarning("[ConfigLoader] ПРИМЕНЕНИЕ ДЕФОЛТНЫХ ЗНАЧЕНИЙ!");
            WeaponConfigList defaultConfig = new WeaponConfigList { weapons = _defaultWeapons };
            ApplyConfig(defaultConfig);
        }
        
        private void SaveLocalCache(string json)
        {
            File.WriteAllText(_localCachePath, json);
            Debug.Log($"[ConfigLoader] Конфиг успешно сохранен в кеш: {_localCachePath}");
        }
        
        private void ApplyConfig(WeaponConfigList config)
        {
            Debug.Log("=== ИТОГОВЫЙ КОНФИГ ОРУЖИЯ ===");
            foreach (var w in config.weapons)
            {
                Debug.Log($"Оружие: {w.id} | Урон: {w.damage} | Кулдаун: {w.cooldown} сек.");
            }
            Debug.Log("===============================");
        }
    }
}