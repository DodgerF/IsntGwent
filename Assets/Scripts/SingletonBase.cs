using UnityEngine;

[DisallowMultipleComponent]
public abstract class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
{
    [Header("Singleton")]
    [SerializeField] private bool m_DontDestroyOnLoad;

    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("MonoSingleton: object is already exists, instance will be destroyed = " + typeof(T).FullName);
            Destroy(this);
            return;
        }

        Instance = this as T;

        if (m_DontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
