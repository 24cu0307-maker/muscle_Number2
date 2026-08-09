Composable Light Set

Base/StageLight_Base.prefab
実際に床・演者・背景を照らすSpot Lightです。
Light Effect ComposerのEffectsは空なので、必要なEffectを自由に追加してください。

Attachments
Effect_SpotlightCone : Spot Lightの半透明Coneだけを追加します。
Effect_Halo          : 光源位置へ柔らかなHaloだけを追加します。
Effect_RadialRays    : Haloとは独立した放射状の光条を追加します。
Effect_Laser         : 細いLaser Beamを追加します。

Attachments/AdditionalLights
Effect_Plain45_Key : 斜め45度のKey Lightです。
Effect_Front_Fill  : 正面から影を弱めるFill Lightです。
Effect_Edge_Rim    : 後方側面から輪郭を出すRim Lightです。
Effect_Under       : 下方向から照らすUnder Lightです。

Presets/StageLight_Spotlight_Halo.prefab
Spot Light本体へSpotlight ConeとHaloを登録済みの組み合わせ例です。
Sceneへ置くだけで使用できます。

任意のEffectを追加する方法

1. StageLight_BaseをSceneへ配置します。
2. Light Effect ComposerのEffectsでSizeを増やします。
3. Effect PrefabへAttachments内のPrefabをドラッグします。
4. Enabled、Local Position、Local Euler Angles、Local Scaleを調整します。
5. さらにSizeを増やすことで、同じEffectを含めて任意の数だけ追加できます。

実際の照明値は同じObjectのLight Componentで変更します。
視覚表現は各Attachment Prefabを複製して値を変更すると、Light本体と独立して調整できます。

EffectsのPrefab欄はLightEffectBase型です。
LightEffectBaseを継承していない通常Prefabは登録できません。
新しいLight演出を追加する場合は、演出ComponentをLightEffectBaseから継承してください。
Effect_Halo.prefab は中心光・ぼかし・光源から離れた発光リングを担当します。
Effect_RadialRays.prefab は放射状の光条だけを担当します。
両方必要な場合はLightEffectComposerのEffectsへ別々に追加してください。
