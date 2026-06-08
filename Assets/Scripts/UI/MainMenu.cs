using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	[Header("Panels")]
	public GameObject mainPanel;

	public GameObject settingsPanel;

	[Header("Settings Controls")]
	public Slider volumeSlider;

	public Slider sensitivitySlider;

	public Slider fovSlider;

	public Toggle fullscreenToggle;

	public Toggle invertXToggle;

	public Toggle invertYToggle;

	public TextMeshProUGUI volumeLabel;

	public TextMeshProUGUI sensitivityLabel;

	public TextMeshProUGUI fovLabel;

	[Header("Graphics Controls")]
	public TMP_Dropdown resolutionDropdown;

	public TMP_Dropdown qualityDropdown;

	public TMP_Dropdown textureDropdown;

	public Toggle vsyncToggle;

	private readonly List<Resolution> _resolutions = new List<Resolution>();

	private GameObject _codeMain;

	private GameObject _codeSettings;

	private GameObject _codeControls;

	private GameObject _startBtn;

	private void Start()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		if (GameManager.Instance == null)
		{
			new GameObject("GameManager").AddComponent<GameManager>();
		}
		SetupGraphicsDropdowns();
		LoadSettings();
		BuildCodeUI();
	}

	public void StartGame()
	{
		GameManager.Instance.StartGame();
	}

	public void QuitGame()
	{
		GameManager.Instance.QuitGame();
	}

	private void Update()
	{
		Keyboard current = Keyboard.current;
		if (current != null && current.escapeKey.wasPressedThisFrame && ((_codeSettings != null && _codeSettings.activeSelf) || (_codeControls != null && _codeControls.activeSelf)))
		{
			ShowMain();
		}
	}

	private void BuildCodeUI()
	{
		if (mainPanel != null)
		{
			mainPanel.SetActive(value: false);
		}
		if (settingsPanel != null)
		{
			settingsPanel.SetActive(value: false);
		}
		Canvas canvas = GetComponentInParent<Canvas>();
		if (canvas == null)
		{
			canvas = Object.FindFirstObjectByType<Canvas>();
		}
		Transform parent = ((canvas != null) ? canvas.transform : base.transform);
		UIKit.Title(parent, "LABYRINTH ESCAPE", 330f);
		_codeMain = UIKit.Box("MainMenuPanel", parent, new Vector2(460f, 520f));
		_startBtn = UIKit.Button(_codeMain.transform, 0f, 150f, 320f, 60f, "Start", StartGame).gameObject;
		UIKit.Button(_codeMain.transform, 0f, 75f, 320f, 60f, "Controls", ShowControls);
		UIKit.Button(_codeMain.transform, 0f, 0f, 320f, 60f, "Settings", OpenSettings);
		UIKit.Button(_codeMain.transform, 0f, -75f, 320f, 60f, "Quit", delegate
		{
			UIKit.Confirm(parent, "Quit the game?", QuitGame);
		});
		TextMeshProUGUI textMeshProUGUI = UIKit.Text(_codeMain.transform, "v" + Application.version, 16f, FontStyles.Normal, TextAlignmentOptions.Center);
		RectTransform rectTransform = textMeshProUGUI.rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 0f);
		rectTransform.anchorMax = new Vector2(0.5f, 0f);
		rectTransform.pivot = new Vector2(0.5f, 0f);
		rectTransform.anchoredPosition = new Vector2(0f, 16f);
		rectTransform.sizeDelta = new Vector2(300f, 24f);
		textMeshProUGUI.color = new Color(UIKit.TextCol.r, UIKit.TextCol.g, UIKit.TextCol.b, 0.5f);
		_codeSettings = SettingsPanel.Create(parent, ShowMain);
		_codeControls = ControlsPanel.Create(parent, ShowMain);
		ShowMain();
	}

	public void OpenSettings()
	{
		_codeMain.SetActive(value: false);
		_codeControls.SetActive(value: false);
		_codeSettings.SetActive(value: true);
		UIKit.SelectFirst(_codeSettings);
	}

	public void ShowControls()
	{
		_codeMain.SetActive(value: false);
		_codeSettings.SetActive(value: false);
		_codeControls.SetActive(value: true);
		UIKit.SelectFirst(_codeControls);
	}

	public void ShowMain()
	{
		if (_codeSettings != null)
		{
			_codeSettings.SetActive(value: false);
		}
		if (_codeControls != null)
		{
			_codeControls.SetActive(value: false);
		}
		if (_codeMain != null)
		{
			_codeMain.SetActive(value: true);
		}
		UIKit.SelectFirst(_startBtn);
	}

	public void OnVolumeChanged(float v)
	{
		AudioListener.volume = v;
		if (volumeLabel != null)
		{
			volumeLabel.text = $"Volume: {Mathf.RoundToInt(v * 100f)}%";
		}
		PlayerPrefs.SetFloat("opt_volume", v);
	}

	public void OnSensitivityChanged(float v)
	{
		if (sensitivityLabel != null)
		{
			sensitivityLabel.text = $"Sensitivity: {v:F1}";
		}
		PlayerPrefs.SetFloat("opt_sensitivity", v);
	}

	public void OnFovChanged(float v)
	{
		if (fovLabel != null)
		{
			fovLabel.text = $"FOV: {Mathf.RoundToInt(v)}";
		}
		GameSettings.SetFov(v);
	}

	public void OnFullscreenChanged(bool b)
	{
		PlayerPrefs.SetInt("opt_fullscreen", b ? 1 : 0);
		ApplyResolution();
	}

	public void OnInvertYChanged(bool b)
	{
		PlayerPrefs.SetInt("opt_invertY", b ? 1 : 0);
	}

	public void OnInvertXChanged(bool b)
	{
		PlayerPrefs.SetInt("opt_invertX", b ? 1 : 0);
	}

	private void SetupGraphicsDropdowns()
	{
		if (resolutionDropdown != null)
		{
			_resolutions.Clear();
			resolutionDropdown.ClearOptions();
			HashSet<string> hashSet = new HashSet<string>();
			List<string> list = new List<string>();
			Resolution[] resolutions = Screen.resolutions;
			for (int i = 0; i < resolutions.Length; i++)
			{
				Resolution item = resolutions[i];
				string item2 = item.width + "x" + item.height;
				if (hashSet.Add(item2))
				{
					_resolutions.Add(item);
					list.Add(item2);
				}
			}
			resolutionDropdown.AddOptions(list);
		}
		if (qualityDropdown != null)
		{
			qualityDropdown.ClearOptions();
			qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
		}
		if (textureDropdown != null)
		{
			textureDropdown.ClearOptions();
			textureDropdown.AddOptions(new List<string> { "Texture: Ultra", "Texture: High", "Texture: Medium", "Texture: Low" });
		}
	}

	public void OnResolutionChanged(int index)
	{
		if (index >= 0 && index < _resolutions.Count)
		{
			PlayerPrefs.SetInt("opt_resW", _resolutions[index].width);
			PlayerPrefs.SetInt("opt_resH", _resolutions[index].height);
			ApplyResolution();
		}
	}

	public void OnQualityChanged(int index)
	{
		QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
		PlayerPrefs.SetInt("opt_quality", index);
	}

	public void OnVsyncChanged(bool b)
	{
		QualitySettings.vSyncCount = (b ? 1 : 0);
		PlayerPrefs.SetInt("opt_vsync", b ? 1 : 0);
	}

	public void OnTextureChanged(int index)
	{
		QualitySettings.globalTextureMipmapLimit = index;
		PlayerPrefs.SetInt("opt_texq", index);
	}

	private void ApplyResolution()
	{
		FullScreenMode fullScreenMode = ((PlayerPrefs.GetInt("opt_fullscreen", 1) == 1) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
		if (PlayerPrefs.HasKey("opt_resW"))
		{
			Screen.SetResolution(PlayerPrefs.GetInt("opt_resW"), PlayerPrefs.GetInt("opt_resH"), fullScreenMode);
		}
		else
		{
			Screen.fullScreenMode = fullScreenMode;
		}
	}

	private void LoadSettings()
	{
		float @float = PlayerPrefs.GetFloat("opt_volume", 0.8f);
		float float2 = PlayerPrefs.GetFloat("opt_sensitivity", 1f);
		float float3 = PlayerPrefs.GetFloat("opt_fov", 80f);
		bool isOnWithoutNotify = PlayerPrefs.GetInt("opt_fullscreen", 1) == 1;
		if (volumeSlider != null)
		{
			volumeSlider.SetValueWithoutNotify(@float);
			OnVolumeChanged(@float);
		}
		if (sensitivitySlider != null)
		{
			sensitivitySlider.SetValueWithoutNotify(float2);
			OnSensitivityChanged(float2);
		}
		if (fovSlider != null)
		{
			fovSlider.SetValueWithoutNotify(float3);
			OnFovChanged(float3);
		}
		if (fullscreenToggle != null)
		{
			fullscreenToggle.SetIsOnWithoutNotify(isOnWithoutNotify);
		}
		if (invertXToggle != null)
		{
			invertXToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("opt_invertX", 0) == 1);
		}
		if (invertYToggle != null)
		{
			invertYToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("opt_invertY", 0) == 1);
		}
		int @int = PlayerPrefs.GetInt("opt_quality", QualitySettings.GetQualityLevel());
		int int2 = PlayerPrefs.GetInt("opt_vsync", 1);
		int int3 = PlayerPrefs.GetInt("opt_texq", 0);
		if (qualityDropdown != null)
		{
			qualityDropdown.SetValueWithoutNotify(@int);
		}
		if (vsyncToggle != null)
		{
			vsyncToggle.SetIsOnWithoutNotify(int2 == 1);
		}
		if (textureDropdown != null)
		{
			textureDropdown.SetValueWithoutNotify(int3);
		}
		if (resolutionDropdown != null)
		{
			int curW = PlayerPrefs.GetInt("opt_resW", Screen.width);
			int curH = PlayerPrefs.GetInt("opt_resH", Screen.height);
			int num = _resolutions.FindIndex((Resolution r) => r.width == curW && r.height == curH);
			if (num >= 0)
			{
				resolutionDropdown.SetValueWithoutNotify(num);
			}
		}
		GameSettings.ApplyGraphics();
	}
}
