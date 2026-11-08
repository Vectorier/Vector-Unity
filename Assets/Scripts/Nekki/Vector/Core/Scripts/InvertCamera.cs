using UnityEngine;
using UnityEngine.Rendering;

namespace Nekki.Vector.Core.Scripts
{
    /// <summary>
    /// Flips the gameplay camera's projection (Vector data is Y-down) and inverts face culling while it renders.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
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

        private void OnBeginCameraRendering(ScriptableRenderContext context, UnityEngine.Camera camera)
        {
            if (camera != _camera)
            {
                return;
            }
            camera.ResetWorldToCameraMatrix();
            camera.ResetProjectionMatrix();
            camera.projectionMatrix = camera.projectionMatrix * Matrix4x4.Scale(_Scale);
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
