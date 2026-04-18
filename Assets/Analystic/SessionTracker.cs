using System;
using System.Collections;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class SessionTracker : MonoBehaviour
{
    private float _sessionStartTime;
    private bool _isSessionEnded = false;

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            AnalyticsService.Instance.StartDataCollection();

            _sessionStartTime = Time.realtimeSinceStartup;
            Debug.Log("เริ่มจับเวลาเล่น");
        }
        catch (Exception e)
        {
            Debug.LogError($"เชื่อมต่อไม่ได้: {e.Message}");
        }
    }




    public void FinishGame()
    {
        SendSessionEvent("Game_Clear");
    }





    public void QuitGameByButton()
    {
        Debug.Log("กดปุ่ม Exit   กำลังส่งข้อมูล");

        StartCoroutine(QuitRoutine());
    }

    private IEnumerator QuitRoutine()
    {
        SendSessionEvent("Quit_By_Button");
        yield return new WaitForSecondsRealtime(0.5f);




#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(); 
#endif
    }



    
    private void OnApplicationQuit()
    {
        SendSessionEvent("GameClosed");
    }



    
    private void SendSessionEvent(string reason)
    {
        if (_isSessionEnded) return;
        _isSessionEnded = true;

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized) return;

            float timePlayed = Time.realtimeSinceStartup - _sessionStartTime;

            CustomEvent sessionData = new CustomEvent("Session_Length")
            {
                { "TimePlayed", timePlayed },
                { "ExitReason", reason }
            };

            AnalyticsService.Instance.RecordEvent(sessionData);

            Debug.Log($"<color=orange>📤 [SENT] Session_Length | เวลาเล่น: {timePlayed:F2} วินาที | สาเหตุ: {reason}</color>");

            AnalyticsService.Instance.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Analytics Error] {ex.Message}");
        }
    }
}