using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSwitchingController
{
    public enum Scene
    {
        Idle,
        StandingExercise,
        StandingTrials,
        WalkingTrials
    }
    
    public static IEnumerator LoadSceneAsync(Scene scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene.ToString());

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
            yield return null;
    }
}