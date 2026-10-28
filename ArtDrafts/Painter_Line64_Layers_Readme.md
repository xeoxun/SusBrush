# 64px 플레이어 색상 레이어

총 32프레임을 각각 Fixed와 Tint로 분리한64장의 투명 PNG입니다. 모든 파일은64x64이며 원본의 픽셀 좌표와 알파 영역을 보존합니다. 기존 원본, 씬, 프리팹, Animator는 변경하지 않았습니다.

## 폴더

- Assets/Sprites/Characters/Painter_Line64_Layers/Fixed/Carry 또는 Draw
- Assets/Sprites/Characters/Painter_Line64_Layers/Tint/Carry 또는 Draw
- 파일명은 원본 이름 뒤에 _Fixed 또는 _Tint를 붙였습니다.

## Unity 연결

1. 같은 부모 아래 SpriteRenderer 두 개를 동일한 위치·크기·회전으로 겹칩니다.
2. Tint는 몸 스킨과 붓끝에 묻은 물감이며, 흰색과 회색으로 만들었습니다. 해당 Renderer.color에 플레이어 색을 지정합니다.
3. Fixed는 선화·눈·얼굴·앞치마·붓 몸체이며 흰색/회색/검정으로 유지합니다. 해당 Renderer.color는 Color.white로 둡니다.
4. Fixed를 Tint보다 높은 Sorting Order로 둡니다.
5. 두 Renderer는 항상 같은 모드, 방향, 동작, 프레임 번호를 함께 재생합니다. 예: Carry_Down_Run_01_Fixed와 Carry_Down_Run_01_Tint.
6. PPU64, Bilinear, 무압축, Mipmap 꺼짐으로 설정했습니다.

앞치마의 노랑·파랑·빨강 얼룩은 흰색 계열로 정리했으며, 고정 영역에는 플레이어 색을 적용하지 않습니다. 검정 선과 명암용 회색은 유지합니다.

## 확인

-32프레임×2레이어=64이미지.
-알파 영역 손실과 두 레이어의 픽셀 중복 없음 확인.
-8개 플레이어 색상 미리보기: ArtDrafts/Painter_Line64_8Colors_Preview.png.
-분리 예시: ArtDrafts/Painter_Line64_LayerParts_Preview.png. 왼쪽부터 Fixed / Tint / 파란색 합성 결과.

제작: imagegen 내장 도구로 무채색 베이스 방향을 확인하고, 실제32프레임은 기존 동작이 달라지지 않도록 원본 픽셀 위치에서 레이어를 분리했습니다. 프롬프트 방향: white and neutral-gray skin and brush paint, white apron/face, preserve dark outlines and exact pose, transparent background.

