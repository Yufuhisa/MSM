using System;
using System.Collections.Generic;
using System.Threading;
using ModdingSystem;
using UnityEngine;

public class WorkshopAssetLoading : UIDialogBox
{
	public WorkshopAssetLoading()
	{
	}

	protected override void Awake()
	{
		base.Awake();
	}

	public void SetLoadingCompleteAction(WorkshopAssetLoading.LoadingCompleteAction inLoadingCompleteAction, SaveFileInfo inSaveFileToLoad = null)
	{
		this.mLoadingCompleteAction = inLoadingCompleteAction;
		this.mSaveFileToLoad = inSaveFileToLoad;
	}

	private void Update()
	{
		if (this.mState == WorkshopAssetLoading.State.WaitingForInstall)
		{
			ModManager modManager = App.instance.modManager;
			List<SteamMod> allUserSubscribedMods = modManager.allUserSubscribedMods;
			bool flag = true;
			for (int i = 0; i < allUserSubscribedMods.Count; i++)
			{
				SteamMod steamMod = allUserSubscribedMods[i];
				if (steamMod.isActive && !steamMod.isInstalled)
				{
					flag = false;
				}
			}
			if (flag)
			{
				this.mState = WorkshopAssetLoading.State.LoadingAssets;
				this.mThread = new Thread(new ThreadStart(this.LoadAssets));
				this.mThread.Start();
			}
		}
		else if (this.mState == WorkshopAssetLoading.State.LoadingAssets && this.mThread != null && Time.unscaledTime - this.mTimeOnEnter > 2f && !this.mThread.IsAlive)
		{
			this.Hide();
			try
			{
				App.instance.assetManager.ReloadAtlases();
				switch (this.mLoadingCompleteAction)
				{
				case WorkshopAssetLoading.LoadingCompleteAction.CreateNewGame:
					Game.instance.StartNewGame();
					break;
				case WorkshopAssetLoading.LoadingCompleteAction.LoadMostRecentSave:
					App.instance.saveSystem.LoadMostRecentFile();
					break;
				case WorkshopAssetLoading.LoadingCompleteAction.LoadSpecificSave:
					App.instance.saveSystem.Load(this.mSaveFileToLoad, false);
					break;
				}
			}
			catch (Exception ex)
			{
				this.mErrorOccured = true;
				global::Debug.Log(ex.ToString(), null);
			}
			App.instance.errorReporter.SetEnabled(true);
			if (this.mErrorOccured)
			{
				UIManager.instance.dialogBoxManager.Show("WorkshopAssetLoadError");
			}
			else
			{
				UIManager.instance.UIBackground.ForceBackgroundChange();
				if (this.mLoadingCompleteAction == WorkshopAssetLoading.LoadingCompleteAction.CreateNewGame)
				{
					UIManager.instance.ChangeScreen("CreatePlayerScreen", UIManager.ScreenTransition.None, 0f, null, UIManager.NavigationType.Normal);
				}
				else if (this.mLoadingCompleteAction == WorkshopAssetLoading.LoadingCompleteAction.ExitingWorkshopScreen)
				{
					UIManager.instance.ChangeScreen("TitleScreen", UIManager.ScreenTransition.None, 0f, null, UIManager.NavigationType.Normal);
				}
			}
			this.mLoadingCompleteAction = WorkshopAssetLoading.LoadingCompleteAction.CreateNewGame;
			this.mSaveFileToLoad = null;
			this.mThread = null;
			this.mState = WorkshopAssetLoading.State.Idle;
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		App.instance.errorReporter.SetEnabled(false);
		this.mTimeOnEnter = Time.unscaledTime;
		this.mErrorOccured = false;
		this.mState = WorkshopAssetLoading.State.WaitingForInstall;
	}

	private void LoadAssets()
	{
		try
		{
			App.instance.assetManager.ReloadAssetsAndDatabases();
		}
		catch (Exception ex)
		{
			this.mErrorOccured = true;
			global::Debug.Log(ex.ToString(), null);
		}
	}

	private float mTimeOnEnter;

	private Thread mThread;

	private WorkshopAssetLoading.State mState;

	private bool mErrorOccured;

	private WorkshopAssetLoading.LoadingCompleteAction mLoadingCompleteAction;

	private SaveFileInfo mSaveFileToLoad;

	public enum LoadingCompleteAction
	{
		CreateNewGame,
		LoadMostRecentSave,
		LoadSpecificSave,
		ExitingWorkshopScreen
	}

	public enum State
	{
		Idle,
		WaitingForInstall,
		LoadingAssets
	}
}
