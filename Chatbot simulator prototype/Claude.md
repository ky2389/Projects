# Claude Code Core Instructions for Unity Project

## 1. Project Context
* We are building a Unity game prototype. The function is maily to combine AI functions with NPCs in game.
* All game logic is written in C#. 
* I'm an independent developer with no sponsor, so we should try to keep everything simple and cheap, and for demonstration purpose instead of a real game.

## 2. CRITICAL RESTRICTIONS (DO NOT VIOLATE)
* **NEVER** edit, create, or delete `.meta` files. Unity manages these automatically. Touching them will break asset references.
* **NEVER** edit `.prefab`, `.unity` (Scenes), `.asset`, or `.mat` files directly. These are serialized by the Editor.
* **NEVER** write code outside of the `/Assets/Scripts/` directory.

## 3. Unity C# Best Practices
* **MonoBehaviours:** Do not use the `new` keyword to instantiate MonoBehaviours. Use `Instantiate()` or add components via `gameObject.AddComponent<T>()`.
* **Performance:** Avoid `GetComponent<>` or `GameObject.Find()` in `Update()`, `FixedUpdate()`, or `LateUpdate()`. Cache these references in `Awake()` or `Start()`.
* **Logging:** Always use `Debug.Log()`, `Debug.LogWarning()`, or `Debug.LogError()`. Do not use `Console.WriteLine()`.
* **Coroutines/Async:** Use Unity Coroutines (`IEnumerator`) for time-based events over multiple frames. If using `async/await`, ensure you are aware of the Unity main thread limitations.

## 4. Workflow with the Human
* I (the human) will manage the Unity Editor, attach scripts to GameObjects, and assign inspector variables. 
* Your job (the agent) is purely to write, debug, and architect the C# scripts, then tell me what I should do inside the inspector.
* Expose variables to the inspector using `[SerializeField]` rather than making variables `public`, unless they truly need to be accessed by other classes.
* Please follow the coding and commenting style of my existing codes, and if possible, you can try to add functionalities into my existing scripts instead of creating new ones.