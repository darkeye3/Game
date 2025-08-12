using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    #region Singleton
    protected static T _instance;

    public static T Instance
    {
        get
        {
            return _instance;
        }
        protected set
        {
            _instance = value;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;

            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);

            return;
        }
    }

    protected void OnDestroy()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = null;
    }
    #endregion
}