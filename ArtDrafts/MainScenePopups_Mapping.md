# MainScene 팝업 UI 이미지

현재 MainScene을 읽어 제작한 이미지입니다. 씬과 프리팹은 변경하지 않았습니다.

- 아이보리 패널, 갈색 테두리, 파스텔 물감 장식.
- 바깥 영역은 투명하며 패널 내부는 밝은 색을 유지합니다.
- 글자 없는 버튼과 입력칸이므로 기존 TMP 텍스트를 사용합니다.
- 이미지 치수는 Canvas 기준 UI 크기이며 55.69946처럼 소수인 치수는 가장 가까운 정수로 반올림했습니다.
- 0 또는 음수인 동적 레이아웃 요소는 공용 상태 크기로 준비했습니다. 탭 217x120, 스크롤 손잡이 14x50, 슬라이더 채움 368x20. 기존 레이아웃은 수정하지 않았습니다.
- Blocker, LoadingPanel, Viewport 등 기능용 마스크는 장식 이미지 교체 대상에서 제외했습니다.
- UI_요소종류_너비x높이.png 형태로 저장했습니다.
- 제작: imagegen 내장 도구. 프롬프트 방향: ivory painter-game UI, rounded chocolate outlines, mint/coral/blue controls, empty text areas, true transparent exterior, native high-resolution source. 긴 패널은 모서리를 유지하는 방식으로 정확한 목표 크기에 맞췄습니다.

## 요소별 연결 목록

| 요소 경로 | 이미지 | 크기 |
|---|---|---|
| Canvas/RoomBrowserPanel | UI_Browser_1920x1080.png | 1920x1080 |
| Canvas/RoomBrowserPanel/SearchArea | UI_Header_1580x94.png | 1580x94 |
| Canvas/RoomBrowserPanel/SearchArea/SearchInputField | UI_Input_550x58.png | 550x58 |
| Canvas/RoomBrowserPanel/SearchArea/RefreshButton | UI_Blue_160x55.png | 160x55 |
| Canvas/RoomBrowserPanel/SearchArea/CreateRoomButton | UI_Mint_200x60.png | 200x60 |
| Canvas/RoomBrowserPanel/RoomListHeader | UI_Header_1580x56.png | 1580x56 |
| Canvas/RoomBrowserPanel/RoomListScrollView/Viewport/Content/RoomListItem | UI_Header_1564x90.png | 1564x90 |
| Canvas/RoomBrowserPanel/RoomListScrollView/Viewport/Content/RoomListItem/JoinButton | UI_Mint_140x60.png | 140x60 |
| Canvas/RoomBrowserPanel/EmptyRoomMessage | UI_Popup_620x180.png | 620x180 |
| Canvas/RoomBrowserPanel/BackBtn | UI_Coral_200x80.png | 200x80 |
| Canvas/CreateRoomPopup/PopupWindow | UI_Popup_720x450.png | 720x450 |
| Canvas/CreateRoomPopup/PopupWindow/RoomNameRow/InputField | UI_Input_380x58.png | 380x58 |
| Canvas/CreateRoomPopup/PopupWindow/PrivateRoomRow/PublicRoom/Background | UI_Checkbox_50x50.png | 50x50 |
| Canvas/CreateRoomPopup/PopupWindow/PrivateRoomRow/PublicRoom/Background/Checkmark | UI_Check_30x30.png | 30x30 |
| Canvas/CreateRoomPopup/PopupWindow/PrivateRoomRow/PrivateRoom/Background | UI_Checkbox_50x50.png | 50x50 |
| Canvas/CreateRoomPopup/PopupWindow/PrivateRoomRow/PrivateRoom/Background/Checkmark | UI_Check_30x30.png | 30x30 |
| Canvas/CreateRoomPopup/PopupWindow/CancelButton | UI_Coral_200x60.png | 200x60 |
| Canvas/CreateRoomPopup/PopupWindow/CreateButton | UI_Mint_200x60.png | 200x60 |
| Canvas/NicknamePopup/PopupWindow | UI_Popup_660x430.png | 660x430 |
| Canvas/NicknamePopup/PopupWindow/NicknameInputField | UI_Input_500x65.png | 500x65 |
| Canvas/NicknamePopup/PopupWindow/ConfirmButton | UI_Mint_200x60.png | 200x60 |
| Canvas/JoinCodePopup/PopupWindow | UI_Popup_520x330.png | 520x330 |
| Canvas/JoinCodePopup/PopupWindow/CodeInput | UI_Input_400x55.png | 400x55 |
| Canvas/JoinCodePopup/PopupWindow/CancelButton | UI_Coral_150x60.png | 150x60 |
| Canvas/JoinCodePopup/PopupWindow/EnterButton | UI_Mint_150x60.png | 150x60 |
| Canvas/SettingsPopup/PopupWindow | UI_Popup_1320x864.png | 1320x864 |
| Canvas/SettingsPopup/PopupWindow/CloseButton | UI_Coral_200x70.png | 200x70 |
| Canvas/SettingsPopup/PopupWindow/ApplyButton | UI_Mint_200x70.png | 200x70 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/TabButtons/Viewport/Content/SoundTabButton | UI_Tab_217x120.png | 217x120 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/TabButtons/Viewport/Content/DisplayTabButton | UI_Tab_217x120.png | 217x120 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/TabButtons/Viewport/Content/LanguageTabButton | UI_Tab_217x120.png | 217x120 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/TabButtons/Scrollbar | UI_ScrollTrack_14x500.png | 14x500 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/TabButtons/Scrollbar/Handle | UI_ScrollHandle_14x50.png | 14x50 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/MasterVolumn/MasterVolumeSlider/Background | UI_SliderTrack_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/MasterVolumn/MasterVolumeSlider/Fill Area/Fill | UI_SliderFill_368x20.png | 368x20 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/MasterVolumn/MasterVolumeSlider/Handle Slide Area/Handle | UI_SliderHandle_30x10.png | 30x10 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/SFXVolumn/SfxVolumeSlider/Background | UI_SliderTrack_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/SFXVolumn/SfxVolumeSlider/Fill Area/Fill | UI_SliderFill_368x20.png | 368x20 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/SFXVolumn/SfxVolumeSlider/Handle Slide Area/Handle | UI_SliderHandle_30x70.png | 30x70 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/BackgroundVolumn /BackgroundVolumeSlider/Background | UI_SliderTrack_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/BackgroundVolumn /BackgroundVolumeSlider/Fill Area/Fill | UI_SliderFill_368x20.png | 368x20 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/SoundPage/BackgroundVolumn /BackgroundVolumeSlider/Handle Slide Area/Handle | UI_SliderHandle_30x10.png | 30x10 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/ScreenMode/ScreenModeDropdown | UI_Input_500x70.png | 500x70 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/ScreenMode/ScreenModeDropdown/Arrow | UI_Arrow_35x28.png | 35x28 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/ScreenMode/ScreenModeDropdown/Template | UI_Input_500x150.png | 500x150 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/ScreenMode/ScreenModeDropdown/Template/Viewport/Content/window/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/ScreenMode/ScreenModeDropdown/Template/Viewport/Content/FullScreen/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/Rsolution/ResolutionDropdown | UI_Input_500x70.png | 500x70 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/Rsolution/ResolutionDropdown/Arrow | UI_Arrow_35x28.png | 35x28 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/Rsolution/ResolutionDropdown/Template | UI_Input_500x200.png | 500x200 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/Rsolution/ResolutionDropdown/Template/Viewport/Content/960/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/Rsolution/ResolutionDropdown/Template/Viewport/Content/1280/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/DisplayPage/Rsolution/ResolutionDropdown/Template/Viewport/Content/1920/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/LanguagePage/LanguadgeMode/LanguageDropdown | UI_Input_500x70.png | 500x70 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/LanguagePage/LanguadgeMode/LanguageDropdown/Template | UI_Input_500x200.png | 500x200 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/LanguagePage/LanguadgeMode/LanguageDropdown/Template/Viewport/Content/Korean/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/LanguagePage/LanguadgeMode/LanguageDropdown/Template/Viewport/Content/English/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/LanguagePage/LanguadgeMode/LanguageDropdown/Template/Viewport/Content/Japanese/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/SettingsPopup/PopupWindow/SettingsBody/SettingsPages/LanguagePage/LanguadgeMode/LanguageDropdown/Template/Viewport/Content/Chenese/Item Background | UI_Input_500x60.png | 500x60 |
| Canvas/GameRulesPopup/PopupWindow | UI_Popup_1220x780.png | 1220x780 |
| Canvas/GameRulesPopup/PopupWindow/PreviousButton | UI_Previous_60x80.png | 60x80 |
| Canvas/GameRulesPopup/PopupWindow/NextButton | UI_Next_60x80.png | 60x80 |
| Canvas/GameRulesPopup/PopupWindow/CloseButton | UI_Close_80x80.png | 80x80 |
| Canvas/RoomEntryNoticePopup/PopupWindow | UI_Popup_520x280.png | 520x280 |
| Canvas/RoomEntryNoticePopup/PopupWindow/CloseBtn | UI_Coral_170x60.png | 170x60 |

