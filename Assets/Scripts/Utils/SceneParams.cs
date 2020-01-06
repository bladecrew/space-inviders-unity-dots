using UnityEngine;

namespace Utils
{
    public static class SceneParams
    {
        private static Bounds _bounds;
        private static bool _boundsInitialized;

        public static Bounds CameraViewParams()
        {
            if (_boundsInitialized)
                return _bounds;

            var mainCamera = Object.FindObjectOfType<Camera>();
            var screenAspect = Screen.width / (float) Screen.height;
            var cameraHeight = mainCamera.orthographicSize * 2;
            _bounds = new Bounds(mainCamera.transform.position,
                new Vector3(cameraHeight * screenAspect, cameraHeight, 0));

            _boundsInitialized = true;

            return _bounds;
        }
    }
}