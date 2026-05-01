using UnityEngine;
using System.Collections.Generic; // Listを使うために追加
using extOSC;

public class HeartBeatManager : MonoBehaviour
{
    // インスペクターで設定するための専用クラス
    [System.Serializable]
    public class OSCInputConfig
    {
        public string label = "Brain (Heartbeat)"; // 識別用の名前
        public int port = 9000;                    // 受信するポート番号
        public string address = "/heart/beat";     // OSCアドレス
    }

    [Header("OSC Receiver Settings")]
    [Tooltip("受信したいポートとアドレスのリストを追加してください")]
    [SerializeField] private List<OSCInputConfig> inputConfigs = new List<OSCInputConfig>();

    [Header("Visual Settings")]
    [SerializeField] private GameObject spherePrefab;
    [SerializeField] private float lifeTime = 0.5f;

    [Header("Forwarding Settings")]
    [SerializeField] private OSCTransmitter vibratorTransmitter;
    [SerializeField] private string vibratorAddress = "/trigger/vibration";

    // プログラムが自動生成したレシーバーを管理するリスト
    private List<OSCReceiver> activeReceivers = new List<OSCReceiver>();

    void Start()
    {
        // インスペクターで設定したリストを順番に処理
        foreach (var config in inputConfigs)
        {
            // 同じポート番号のレシーバーが既に作られていないかチェック
            OSCReceiver rx = activeReceivers.Find(r => r.LocalPort == config.port);
            
            // まだそのポートを開いていなければ、新しくコンポーネントを追加
            if (rx == null)
            {
                rx = gameObject.AddComponent<OSCReceiver>();
                rx.LocalPort = config.port;
                activeReceivers.Add(rx);
            }

            // 開いたポート（レシーバー）に、アドレスと関数を紐づける
            rx.Bind(config.address, OnReceiveBeat);
            
            Debug.Log($"<color=cyan>[OSC Ready]</color> Listening to '{config.label}' -> Port: {config.port}, Address: {config.address}");
        }
        // ★ Transmitterを明示的に接続
        if (vibratorTransmitter != null)
        {
            vibratorTransmitter.Connect();
            Debug.Log($"Transmitter connected to: {vibratorTransmitter.RemoteHost}:{vibratorTransmitter.RemotePort}");
        }
    }

    private void OnReceiveBeat(OSCMessage message)
    {
        Debug.Log($"Beat received!");
        SpawnSphere();

        Debug.Log($"About to send to {vibratorTransmitter.RemoteHost}:{vibratorTransmitter.RemotePort}");
        
        if (vibratorTransmitter != null)
        {
            var forwardMsg = new OSCMessage(vibratorAddress);
            forwardMsg.AddValue(OSCValue.Int(1));
            vibratorTransmitter.Send(forwardMsg);
            Debug.Log($"Send() called.");
        }
        else
        {
            Debug.LogError("vibratorTransmitter is NULL!");
        }
    }

    private void SpawnSphere()
    {
        // ランダムな位置、あるいは固定位置に球を生成
        Vector3 randomPos = Random.insideUnitSphere * 2f;
        GameObject sphere = Instantiate(spherePrefab, randomPos, Quaternion.identity);
        
        // 指定秒数後に消去
        Destroy(sphere, lifeTime);
    }
}
