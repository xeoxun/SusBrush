# GameScene 헤더 UI

현재 Canvas/Header의 요소 크기에 맞춘 이미지입니다. 부모 Header는 1920x150이지만 하위 정보창은 높이 200으로 되어 있으므로 각 하위 RectTransform의 실제 크기를 사용했습니다. 씬이나 프리팹 크기는 수정하지 않았습니다.

테두리 바깥은 투명하며 내부는 밝은 아이보리입니다. 글자는 기존 UI 텍스트를 사용합니다. imagegen 내장 도구로 제작했습니다. 프롬프트 방향: compact ivory HUD cards, rounded chocolate outlines, tiny pastel paint accents, empty text area, opaque interior and true transparent exterior. 설정 아이콘은 기존 고해상도 원본을 재사용했습니다.

| 요소 | 파일 | 픽셀 |
|---|---|---|
| Canvas/Header/RoomPanel | Header_Room_400x200.png | 400x200 |
| Canvas/Header/TimerPanel | Header_Timer_480x200.png | 480x200 |
| Canvas/Header/RolePanel | Header_Role_350x200.png | 350x200 |
| Canvas/Header/SettingsButton | Header_Settings_100x100.png | 100x100 |

