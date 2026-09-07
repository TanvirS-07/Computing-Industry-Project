using System.IO;
using UnityEngine;

public class FileManager : MonoBehaviour
{
#region Constant Settings

	// These are the settings that have to be changed here
	// ===================================================
	public static string KEYBIND_FILE_PATH => Application.streamingAssetsPath;

#endregion

#region Singleton
	private static FileManager instance;
	[HideInInspector] public static FileManager Instance
	{
		get { return instance; }
		private set
		{
			// if the singleton doesn't already have an instance (or we can replace it)
			if(instance == null)
			{
				instance = value;
				instance.InitSingleton();
				return;
			}
			// if we are the instance
			if(instance == value)
			{
				return;
			}
			Debug.Log($"Note: \"{nameof(FileManager)}\" instance exists, destroying new one!");
			// destroy the new component instance's GameObject
			//   (because the whole prefab is one instance).
			Destroy(value.gameObject);
		}
	}
	/// <summary>
	/// To be used instead of checking Instance == null,
	/// as this can be hijacked for testing purposes.
	/// </summary>
	public static bool InstanceExists() => (instance != null);
#endregion
	
#region Singleton Initialisation
	// Awake() is called the moment the object is active in the scene
	protected virtual void Awake()
	{
		Instance = this;
	}
	/// <summary>
	/// Acts as a stand-in for Start()/Awake(), should only be called by the Singleton.
	/// </summary>
	private void InitSingleton()
	{

	}
#endregion

#if UNITY_EDITOR
	[Header("There are settings inside the script that\n"
			+ "Unity won't show in the inspector, they\n"
			+ "are at the top of \""+nameof(FileManager)+".cs\"")]
	[Space(15)]
	[Header(nameof(FileManager)+" Settings")]
	[HideInInspector] public bool thisIsHerePurelyToHaveTheHeadersAppearInInspector;
#endif

	//[Header("Exposed values, not settings")]
	//private string exitPassword;

	public string ReadFile(string filePath, string filename)
	{
		string completePath = Path.Combine(filePath, filename);

		// Regular file path on most platforms and in Editor
		if(!File.Exists(completePath))
		{
#if UNITY_EDITOR
			Debug.LogError($"ERROR: File not found at: \"{completePath}\"!");
#else
			Debug.LogError($"ERROR: File not found at: \"[FILEPATH EXPUNGED]{Path.DirectorySeparatorChar}{filename}\"!");
#endif
			return "";
		}

		string contents = File.ReadAllText(completePath);

#if UNITY_EDITOR
			Debug.Log($"File \"{filename}\" read at: \"{completePath}\".");
#else
			Debug.Log($"File \"{filename}\" read at: \"[FILEPATH EXPUNGED]{Path.DirectorySeparatorChar}{filename}\".");
#endif

		return contents;
	}


	public void WriteFile(string filePath, string contentsToWrite, bool overwrite)
	{
		if(File.Exists(filePath))
		{
			if(overwrite)
			{
				File.Delete(filePath);
			}
			else
			{
				Debug.Log($"File exists! Set to not overwrite! ({filePath})");
				return;
			}
		}

		StreamWriter f = File.CreateText(filePath);
		f.AutoFlush = true; // Automatically flush buffer to file as otherwise only 0x2000 bytes are written

		f.Write(contentsToWrite);

		f.Close();
	}
}
