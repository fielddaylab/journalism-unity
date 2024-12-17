
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Journalism;

/// <summary>
/// Note: This class is designed to be used with the `vault-floating-dropdown` jslib pluggin. 
/// This script will remove the dropdown elment from the DOM after the target scene has been unloaded.
/// </summary>
public class VaultDropdownToggle : MonoBehaviour {
    [Tooltip("The cutoff level after which the Vault Dropdown will be removed")]
    public int cutoffLevel = 1;

    [DllImport("__Internal")]
    private static extern void DisableVaultButton();

    private void Awake() {
        Game.Events.Register(GameEvents.LevelStarted, OnLevelStarted, this);
    }

    private void OnLevelStarted() {
        int level_started = Assets.CurrentLevel.LevelIndex;

        if (level_started < cutoffLevel) return;
        DisableVaultButton();
    }
    
}