[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/Lvs6kcL8)

# KMU Coop Defense

> 국민대학교 2026 캡스톤디자인 12팀

슈터(FPS)와 서포터(RTS)가 협력하는 멀티플레이 타워 디펜스 게임입니다.

---

## 1. 프로젝트 소개

**KMU Coop Defense**는 두 명의 플레이어가 각각 다른 역할을 맡아 협력하는 비대칭 멀티플레이 게임입니다.

- **슈터 (FPS 시점)**: 1인칭 시점으로 직접 전투에 참여하고, 정화 구역 안에서만 사격 피해를 줄 수 있습니다.
- **서포터 (RTS 시점)**: 탑다운 시점으로 맵 전체를 내려다보며 터렛, 정화 건물 등 구조물을 설치해 슈터를 지원합니다.

두 역할의 유기적인 협력 없이는 밀려오는 적을 막을 수 없습니다. 적의 스폰 코어를 파괴하고 본진 커맨드 타워를 지켜내는 것이 목표입니다.

### 기술 스택

| 항목 | 내용 |
|------|------|
| 엔진 | Unity 2022.3 LTS |
| 네트워크 | Photon PUN 2, Photon Voice 2 |
| 렌더링 | URP (Universal Render Pipeline) |
| 언어 | C# |

### 주요 기능

- 슈터 / 서포터 비대칭 역할 분리
- Photon PUN 2 기반 실시간 멀티플레이
- 정화 구역 시스템 (어둠 노출 피해 / 시야 제한 / 안개 렌더링)
- 서포터 건설 시스템 (터렛, LightPylon, 정화 비콘 등)
- SpawnCore 기반 적 웨이브 시스템
- 음성 채팅, 미니맵 핑 시스템

---

## 2. 소개 영상

추후 추가 예정입니다.

---

## 3. 팀 소개

| 이름 | 학번 | 역할 |
|------|------|------|
| 구자빈 (팀장) | 20203028 | 슈터 플레이어 & 게임 시스템, 적 AI, 오디오, 네트워크 |
| 전경진 | 20203129 | 서포터 플레이어 & 건설 시스템, 정화 시스템, 맵 & 렌더링 |

---

## 4. 사용법

### 실행 환경

- Unity 2022.3.62f3
- Photon PUN 2 (패키지 포함)

### 에셋 설치

프로젝트에는 용량 문제로 일부 에셋이 포함되어 있지 않습니다.  
아래 링크에서 다운로드 후 `Assets/Z_Assets` 경로에 배치하세요.

**에셋 다운로드**: [Google Drive](https://drive.google.com/drive/folders/1Srj95mDoe_thJmNP6JVgQyLZSnRQ4EBt?usp=drive_link)

```
압축 해제 위치: Assets/Z_Assets
```

### 실행 방법

1. Unity Hub에서 프로젝트 열기
2. `Assets/Z_Assets` 에셋 배치 확인
3. `Assets/Project/Scenes/MultiPlayScene` 씬 실행
4. 방 생성 또는 참가 후 슈터 / 서포터 역할 선택

---

## 5. 기타

- 개발 기간: 2025.09 ~ 2026.06
- 소속: 국민대학교 소프트웨어학부 캡스톤디자인
