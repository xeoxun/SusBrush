# SusBrush 게임 진행 및 편집 안내

## 확정 규칙

- 4~8명 전원 준비 후 시작한다. 인원에 관계없이 최대 2라운드다.
- 게임 최초에만 직업·제시어를 배정하고 5초 공개한다. 정신병자는 다른 제시어를 받는 시민이며 배정은 게임 내내 유지한다.
- 매 라운드: 3 → 2 → 1 → Start(각 1초) → 그림 40초 → 투표 20초 → 단독 최다 득표자가 있으면 변론 10초 → 처형.
- 동점·무투표는 변론과 처형 없이 다음 라운드로 진행한다. 마지막 라운드까지 승패가 나지 않으면 무승부다.
- 살아 있는 모든 직업은 생존자와 자기 자신에게 투표할 수 있다. 다른 대상을 클릭하면 표가 이동한다. 사망자는 투표할 수 없다.
- 마피아 처치·투표 처형은 시민 승리, 마피아 정답은 마피아 승리로 게임 전체를 즉시 종료한다.
- 생존자가 시민 진영(경비원·다른 제시어 시민 포함) 1명과 마피아 1명만 남으면 즉시 마피아 승리로 게임 전체를 종료한다. 마피아 사망 시에는 시민 승리를 우선한다.
- 경비원은 그림 시간에만 사격할 수 있다. 게임 전체 2발이며 라운드 전환 시 충전하지 않는다.
- 마피아는 투표 중 F로 입력창을 열고 횟수·시도 간격 제한 없이 정답을 제출한다. 오답은 모든 플레이어에게 공개된다.
- 사망자는 게임 종료까지 관전한다. 사격 사망은 명중 위치에 물감 자국을 남기고, 투표 처형은 캐릭터만 숨긴다.
- 그림과 물감 자국은 게임 시작·새 라운드 시작·게임 종료에 함께 초기화한다. 대기 중에는 이동과 연습 그림을 허용한다.
- 플레이 중 참가자가 나가면 게임을 중단한다. 호스트가 나가면 방을 닫는다.

## 하이어라키와 Inspector 편집

| 대상 | 편집 위치 |
|---|---|
| 그림·투표·변론 시간, 최초 공개 시간, 게임 전체 탄약 | Assets/Settings/MatchRules.asset |
| 제시어·카테고리·질문 | Assets/Settings/KeywordContentCatalog.asset |
| 최초 스폰·투표 위치 | DrawingArea의 Spawn Points에 연결된 Transform |
| 판 범위 | DrawingArea의 World Bounds에 연결된 BoxCollider2D |
| 이동·커서 추적 속도 | Player.prefab / NetworkPlayer / Movement Speed |
| 총알 속도 | Bullet.prefab / Bullet / Speed |
| 투표 표수 텍스트 위치·폰트 | Player.prefab / VoteCount |
| 투표 선택 강조 색상·크기 | Player.prefab / VoteHighlight |
| 사망 자국 크기·이름 위치 | PaintDeathMark.prefab |
| 카테고리 | Canvas / Header / CategoryPanel, RoundView의 Category Text |
| 직업별 조작 안내·잔탄 표시 | MajorUI의 Citizen / Guide / Mafia, GameplayNoticeView 참조 |
| 투표·변론·오답 안내 문구와 페이드 | Canvas / GameplayNoticeView |
| RoomInfo 평소 문구 | RoomHud의 Room Info에 연결된 TMP 텍스트 |
| 효과음 파일과 배경음 연결 | SceneAudio |

RoomInfo의 평소 문구는 코드에서 만들지 않는다. Awake에서 작성된 문구를 저장하고 Spacebar를 누르는 동안만 방 이름과 참가 코드를 표시한다. UI 위치는 런타임 코드에서 변경하지 않는다.

Paint_Death_Splat_Outline.png는 원본 자국 테두리다. Paint_Death_Splat_Fill.png는 이 테두리의 닫힌 내부를 채운 마스크다. Fill에 플레이어 색상을 적용하고 Outline은 원본 색상을 유지한다.

## 책임 분리

- RoundController: 서버 단계·마감 시각, 최초 배정, 공개 카테고리, 즉시 승패, 라운드 전환.
- VotingController: 서버 투표 검증·재집계, 동점 판정, 변론 대상과 처형.
- PlayerVote: 소유자만 호출하는 투표 RPC, 소유자 전용 투표 대상, 전체 공개 표수.
- PlayerVoteView: 표수와 본인 선택 강조. 표수 판정이나 직업 공개를 하지 않는다.
- PlayerRole / PlayerCombat / PlayerGuess: 생존·조작 조건, 사격과 잔탄, 투표 중 마피아 답안 검증.
- PaintMarks: NetworkList로 명중 자국 기록을 동기화하고 늦은 참가자에게 복원한다.
- PaintMarkView: 자국 색상과 닉네임 표시.
- RoundView / GameplayNoticeView / RoundCountdownView: 최초 공개·정답 입력 UI, 공지·직업 안내, 서버 시각 기준 카운트다운.
- SceneUIShortcuts: Enter로 닫힌 채팅 열기, Tab 토글, Esc는 채팅 닫기를 우선하고 닫혀 있을 때만 설정 토글.
- RoomHud: Spacebar 방 정보 표시와 나가기.
- LiarPainterSessionManager / GameScenePlayerSpawner: UGS 방 생성·입장과 NGO 씬 전환·플레이어 생성. 실제 접속은 MainScene에서 시작한다.

## 검증 범위

2026-10-22 로컬 호스트의 플레이어 오브젝트 4개로 게임 단계를 앞당겨 다음을 확인했다.

- 40초 그림·20초 투표·10초 변론 설정, 2라운드 고정, 최초 직업·제시어 유지.
- 자기 투표, 대상 변경, 사망자 차단, 동점·무투표 처리, 시민 처형 후 다음 라운드.
- 마피아 처형과 정답 제출의 게임 전체 즉시 종료, 반복 오답 공개 이벤트.
- 탄약 2발 지급과 라운드 전환 후 잔탄 유지, 투표 중 사격 차단.
- 명중 위치·플레이어 색상으로 사격 자국 생성, 투표 처형에서 자국 추가 생성 안 함.
- 실제 렌더 화면의 표수·선택 강조·공개 카테고리 표시.

이는 서로 다른 4개 클라이언트 접속 시험이 아니다. 원격 투표·오답·자국 수신과 키보드·마우스 전체 사용자 흐름은 실제 다중 클라이언트로 추가 검증할 수 있다. 검증용 플레이어와 스크립트는 제품 씬에 남기지 않는다.

