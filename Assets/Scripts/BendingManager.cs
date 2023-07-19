using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class BendingManager : MonoBehaviour
{
  private static BendingManager _instance;

  #region Constants

  private const string BENDING_FEATURE = "ENABLE_BENDING";

  private const string PLANET_FEATURE = "ENABLE_BENDING_PLANET";

  private static readonly int BENDING_AMOUNT =
    Shader.PropertyToID("_BendingAmount");
    
  private static readonly int WORLD_OFFSET =
    Shader.PropertyToID("_WorldOffset");

  #endregion


  #region Inspector

  [SerializeField]
  private bool enablePlanet = default;

  [SerializeField]
  [Range(-0.1f, 0.1f)]
  private float bendingAmount = 0f;

  public float frontOffset = 0f;

  #endregion


  #region Fields

  private float _prevAmount;
  private float _prevFrontOffset;

  #endregion


  #region MonoBehaviour

  public static BendingManager getInstance()
  {
      return _instance ? _instance : null;
  }


  private void Awake ()
  {
    _instance = this;
    if ( Application.isPlaying )
      Shader.EnableKeyword(BENDING_FEATURE);
    else
      Shader.DisableKeyword(BENDING_FEATURE);

    if ( enablePlanet )
      Shader.EnableKeyword(PLANET_FEATURE);
    else
      Shader.DisableKeyword(PLANET_FEATURE);

    UpdateBendingAmount();
  }

  private void OnEnable ()
  {
    if ( !Application.isPlaying )
      return;
    
    RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
  }

  private void Update ()
  {
    if ( Math.Abs(_prevAmount - bendingAmount) > Mathf.Epsilon )
      UpdateBendingAmount();
    if ( Math.Abs(_prevFrontOffset - frontOffset) > Mathf.Epsilon )
      UpdateWorldOffsetAmount();
  }

  private void OnDisable ()
  {
    RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
  }

  #endregion


  #region Methods

  private void UpdateBendingAmount ()
  {
    _prevAmount = bendingAmount;
    Shader.SetGlobalFloat(BENDING_AMOUNT, bendingAmount);
  }

  private void UpdateWorldOffsetAmount ()
  {
    _prevFrontOffset = frontOffset;
    Shader.SetGlobalVector(WORLD_OFFSET, new Vector4(0, 0, frontOffset, 0));
  }

  private static void OnBeginCameraRendering (ScriptableRenderContext ctx,
                                              Camera cam)
  {
    cam.cullingMatrix = Matrix4x4.Ortho(-99, 99, -99, 99, 0.001f, 99) *
                        cam.worldToCameraMatrix;
  }

  private static void OnEndCameraRendering (ScriptableRenderContext ctx,
                                            Camera cam)
  {
    cam.ResetCullingMatrix();
  }

  #endregion
}