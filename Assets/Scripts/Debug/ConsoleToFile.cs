using UnityEngine;
using System.IO;
using System;
using TMPro;

[DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
public class ConsoleToFile : MonoBehaviour
{
#region Constant Settings

	// These are the settings that have to be changed here
	// ===================================================
	// NOT using nameof(ConsoleToFile) for the string as renaming the class would change this string but wouldn't rename the actual file to match.
	public static string CONSOLETOFILE_ENABLED_FILE_NAME = "ConsoleToFileEnabled.txt";

#endregion

	//[Header("This "+nameof(GameObject)+" will destroy\nitself if not writing to a file!")]
	[SerializeField] private string consoleToFileActivatedText = "In addition to the playtesting form provided after this\nplaytest, we are gathering <u>NON-PERSONAL</u> information\nby writing to a file (filename below). This includes:\n\n- What you interact with and when, to help us\nunderstand and improve user experience;\n\n- Potential errors, debugging, and other setup info\nto ensure the game is running as intended.\n\nIf you are running this game on your own computer,\nplease provide this file to us (we will tell direct you to the file):\n%REPLACEBYCODE%\n\nYou have the right to have this file deleted if you\ndo not agree to having this information saved.";
	[SerializeField] private string consoleToFileDeactivatedText = "Please note, we are NOT recording any debug information to a file, as the file could not be created.\n\nAs such, please remember to fill out the playtesting form provided after the playtest.";
	private static string filepath;
	private static StreamWriter file = null;
	private static bool available = false;
	private static bool currentlyOpen = false;

	private static string FOLDER_PATH => Application.streamingAssetsPath;//Application.persistentDataPath;

	// https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ConsoleToFileListenForLogs()
	{
#if !UNITY_EDITOR
		// If the FileManager exists, check for the settings file.
		if(FileManager.InstanceExists())
		{
			string s = FileManager.Instance.ReadFile(FileManager.KEYBIND_FILE_PATH, CONSOLETOFILE_ENABLED_FILE_NAME);

			// No need to check for the string being null/empty/whitespace as that is all caught by the catch block.
			try
			{
				bool.TryParse(s, out available);
			}
			catch
			{
				Debug.LogWarning($"Warning: Couldn't parse the contents of \"{CONSOLETOFILE_ENABLED_FILE_NAME}\", defaulting to {false}!");
				available = false;
			}

			// If not available, stop here and don't try to create the file.
			if(!available)
			{
				return;
			}
		}

		// Try to create the file. If it suceeded, ConsoleToFile functionality is available.
		available = TryCreateFile();
#else
		available = false;
#endif
	}

	private static void LogMessageListener(string condition, string stackTrace, LogType type)
	{
		if(!available) { return; }
		if(!currentlyOpen) { OpenCloseFile(true); }

		file.WriteLine(type + " | " + Time.realtimeSinceStartupAsDouble);
		file.WriteLine(condition);
		file.WriteLine(stackTrace);
	}

	private void OnApplicationPause(bool pause)
	{
		Debug.Log($"Unity event {nameof(OnApplicationPause)}({pause}) occured");
		OpenCloseFile(!pause);
	}
	private void OnApplicationFocus(bool focusStatus)
	{
		Debug.Log($"Unity event {nameof(OnApplicationFocus)}({focusStatus}) occured");
		OpenCloseFile(focusStatus);
	}
	private void OnApplicationQuit()
	{
		Debug.Log($"Unity event {nameof(OnApplicationQuit)}() occured");
		OpenCloseFile(false);
	}

	// returns if succeeded
	public static bool TryCreateFile()
	{
		Debug.Log($"{nameof(ConsoleToFile)} attempting to create file");

		if(FOLDER_PATH == null || FOLDER_PATH == "" || FOLDER_PATH.Trim() == "")
		{
#if UNITY_EDITOR
			// Don't log this in builds. This can't be seen in builds anyway.
			Debug.Log($"{nameof(ConsoleToFile)} NO FOLDER PATH \"{nameof(FOLDER_PATH)}\"!");
#endif
			return false;
		}

		string dirpath = FOLDER_PATH + Path.DirectorySeparatorChar + nameof(ConsoleToFile);//Path.GetFullPath();
		string filename = nameof(ConsoleToFile)+"_"+(System.DateTime.UtcNow.ToString("s").Replace(":", "."))+".txt";

		foreach(char invalidChar in Path.GetInvalidPathChars())
		{
			dirpath.Replace(invalidChar, '_');
			filename.Replace(invalidChar, '_');
		}
		foreach(char invalidChar in Path.GetInvalidFileNameChars())
		{
			filename.Replace(invalidChar, '_');
		}

		filepath = dirpath + Path.DirectorySeparatorChar + filename; //Path.Combine(dirpath, filename);


		try
		{
			if(!Directory.Exists(dirpath))
			{
				// If the directory doesn't exist, create it.
				DirectoryInfo dir = Directory.CreateDirectory(dirpath);
				if(!dir.Exists)
				{
					// If we failed to create it, something is wrong.
#if UNITY_EDITOR
					// Don't log this in builds. This can't be seen in builds anyway.
					Debug.Log($"{nameof(ConsoleToFile)} {nameof(FOLDER_PATH)} COULDN'T BE CREATED (\"{dirpath}\")");
#endif
					filepath = "";
					file = null;
					return false;
				}
			}

			file = new StreamWriter(filepath);
			currentlyOpen = true;

			file.WriteLine(filename);
			file.WriteLine("--------------------------------");
			file.WriteLine("Log Type | Seconds since startup");
			file.WriteLine("Message");
			file.WriteLine("Stacktrace");
			file.WriteLine("--------------------------------");
			file.WriteLine();

#if UNITY_EDITOR
			Debug.Log($"{nameof(ConsoleToFile)} LISTENING, WILL OUTPUT TO \"{filepath}\".");
#else
			Debug.Log($"{nameof(ConsoleToFile)} LISTENING, WILL OUTPUT TO \"[FILEPATH EXPUNGED]{Path.DirectorySeparatorChar}{filename}\".");
#endif

			Application.logMessageReceived += LogMessageListener;
		}
		catch(Exception e)
		{
#if UNITY_EDITOR
			Debug.Log($"{nameof(ConsoleToFile)} FAILED, NEXT {nameof(Debug.Log)} IS THE EXCEPTION MESSAGE");
			Debug.Log(e); // log instead
#else
			Debug.LogWarning($"{nameof(ConsoleToFile)} FAILED, NEXT {nameof(Debug.LogWarning)} IS THE EXCEPTION MESSAGE");
			Debug.LogWarning(e); // log as warning instead
#endif
			file = null;
			currentlyOpen = false;
			Application.logMessageReceived -= LogMessageListener;
			return false;
		}

		return true;
	}

	public static void OpenCloseFile(bool open)
	{
		if(!available) { return; }
		if(currentlyOpen == open) { return; }

		if(open)
		{
			file = new StreamWriter(filepath, true);
			currentlyOpen = true;
		}
		else
		{
			if(file != null)
			{
				file.Dispose();
			}
			file = null;
			currentlyOpen = false;
		}
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	private void Start()
	{
		OpenCloseFile(true);

		if(!available)
		{
			// make sure it isn't listening anymore
			Application.logMessageReceived -= LogMessageListener;
		}

		if(!CenterTextBoard.InstanceExists()) {  return;  }
		if(CenterTextBoard.Instance.ShowType != CenterTextBoard.CenterTextBoardShowType.CONSOLETOFILE) {  return;  }

		if(available)
		{
#if UNITY_EDITOR
			CenterTextBoard.Instance.TextBoard.text = consoleToFileActivatedText.Replace("%REPLACEBYCODE%", filepath);
#else
			// Expunge the file path
			string expungedFilepath = "[FILEPATH EXPUNGED]" + Path.DirectorySeparatorChar
				// keep only the filename
				+ filepath.Split(Path.DirectorySeparatorChar)[^1];
			CenterTextBoard.Instance.TextBoard.text = consoleToFileActivatedText.Replace("%REPLACEBYCODE%", expungedFilepath);
#endif
		}
		else
		{
			CenterTextBoard.Instance.TextBoard.textWrappingMode = TextWrappingModes.Normal;
			CenterTextBoard.Instance.TextBoard.text = consoleToFileDeactivatedText;
		}
	}
/*
	private static void A()
	{
		// https://discussions.unity.com/t/android-writing-to-application-persistentdatapath/51432/5
		string path = "";
#if UNITY_ANDROID && !UNITY_EDITOR || true
		try
		{
			IntPtr obj_context = AndroidJNI.FindClass("android/content/ContextWrapper");
			IntPtr method_getFilesDir = AndroidJNIHelper.GetMethodID(obj_context, "getFilesDir", "()Ljava/io/File;");

			using(AndroidJavaClass cls_UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				using(AndroidJavaObject obj_Activity = cls_UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
				{
					IntPtr file = AndroidJNI.CallObjectMethod(obj_Activity.GetRawObject(), method_getFilesDir, new jvalue[0]);
					IntPtr obj_file = AndroidJNI.FindClass("java/io/File");
					IntPtr method_getAbsolutePath = AndroidJNIHelper.GetMethodID(obj_file, "getAbsolutePath", "()Ljava/lang/String;");   

					path = AndroidJNI.CallStringMethod(file, method_getAbsolutePath, new jvalue[0]);                    

					if(path != null)
					{
						Debug.Log("Got internal path: " + path);
					}
					else
					{
						Debug.Log("Using fallback path");
						path = "/data/data/*** YOUR PACKAGE NAME ***//*files";
					}
				}
			}
		}
#endif
	}*/
}