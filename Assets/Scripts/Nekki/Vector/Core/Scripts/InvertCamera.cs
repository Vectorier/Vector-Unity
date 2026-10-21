using UnityEngine;
using UnityEngine.Rendering;

namespace Nekki.Vector.Core.Scripts
{
    public class InvertCamera : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _Scale = new Vector3(1f, -1f, 1f);

        private UnityEngine.Camera _camera;
        private bool _previousInvertCulling;
        private bool _cullingChanged;

        private void OnEnable()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RestoreCulling();
            if (_camera != null)
            {
                _camera.ResetProjectionMatrix();
            }
        }

        private void OnPreCull()
        {
            ApplyInversion(_camera);
        }

        private void OnPreRender()
        {
            _previousInvertCulling = GL.invertCulling;
            GL.invertCulling = true;
            _cullingChanged = true;
        }

        private void OnPostRender()
        {
            RestoreCulling();
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, UnityEngine.Camera camera)
        {
            if (camera != _camera)
            {
                return;
            }

            ApplyInversion(camera);
            _previousInvertCulling = GL.invertCulling;
            GL.invertCulling = true;
            _cullingChanged = true;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, UnityEngine.Camera camera)
        {
            if (camera != _camera)
            {
                return;
            }

            RestoreCulling();
            camera.ResetProjectionMatrix();
        }

        private void ApplyInversion(UnityEngine.Camera camera)
        {
            camera.ResetWorldToCameraMatrix();
            camera.ResetProjectionMatrix();
            camera.projectionMatrix = camera.projectionMatrix * Matrix4x4.Scale(_Scale);
        }

        private void RestoreCulling()
        {
            if (_cullingChanged)
            {
                GL.invertCulling = _previousInvertCulling;
                _cullingChanged = false;
            }
        }
    }
}
