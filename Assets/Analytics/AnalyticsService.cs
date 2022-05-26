#if UNITY_WEBGL && !UNITY_EDITOR
#define FIREBASE
#endif // UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.InteropServices;
using UnityEngine;

namespace Journalism
{
    public static class AnalyticsService
    {

        #region Firebase JS Functions

        [DllImport("__Internal")]
        public static extern void FBGameStart();

        #endregion // Firebase JS Functions

        private static void LogGameStarted()
        {
            #if FIREBASE
                FBGameStart();
            #endif
        }
    }
}
