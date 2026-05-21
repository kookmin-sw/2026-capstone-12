[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/Lvs6kcL8)

# KMU Coop Defense

> 국민대학교 2026 캡스톤디자인 12팀

![Poster](docs/poster.png)

슈터(FPS)와 서포터(RTS)가 협력하는 멀티플레이 타워 디펜스 게임입니다.

---

## 1. 프로젝트 소개


**KMU Coop Defense**는 오염의 근원인 **SpawnCore**가 정화 에너지를 흡수해 빛을 잃은 전장을 배경으로 하는 2인 협동 비대칭 멀티플레이 게임입니다.

- **슈터 (FPS 시점)**: 1인칭 FPS 시점으로 직접 전투에 참여하고, 정화 구역 안에서는 정상 사격 피해를 줄 수 있습니다. (정화되지 않은 구역에서는 제한된 피해)
- **서포터 (RTS 시점)**: 탑다운 RTS 시점으로 맵 전체를 내려다보며 터렛, 정화 건물 등 구조물을 설치해 슈터를 지원합니다.

두 역할의 유기적인 협력 없이는 밀려오는 적을 막을 수 없습니다. 
협력하여 잃어버린 빛을 되찾고, 적의 SpawnCore를 파괴하며, 지역 방어 거점인 CommandTower를 지켜내야 합니다.

---

### 게임 설정

이 전장은 오염 확산을 저지하기 위해 세워진 지역 방어 구역입니다.  
그러나 오염의 근원인 **SpawnCore**가 주변의 정화 에너지를 흡수하면서, 구역은 점차 빛을 잃고 어둠에 잠기기 시작했습니다.

플레이어는 이 지역의 방어 거점인 **CommandTower**를 지키기 위해 투입된 2인 작전팀입니다.  
슈터는 전장에 직접 나가 적과 SpawnCore를 공격하고, 서포터는 LightPylon, 정화 비콘, 터렛 등의 구조물을 설치해 정화 구역을 확장하며 슈터를 지원합니다.

적을 처치하면 정화 에너지를 회수할 수 있으며, 이 에너지는 빛을 되찾고 방어선을 유지하는 핵심 자원이 됩니다.

최종 목표는 모든 **SpawnCore**를 파괴해 오염 확산을 막고, **CommandTower**를 끝까지 지켜내는 것입니다.

---

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
- SpawnCore 기반 적 생성 시스템
- 음성 채팅, 미니맵 핑 시스템

---

## 2. 게임 시연 영상

[![게임 시연 영상](https://img.youtube.com/vi/tHrTTPJ-C20/maxresdefault.jpg)](https://www.youtube.com/watch?v=tHrTTPJ-C20)

---

## 3. 팀 소개

| 이름 | 학번 | 역할 |
|------|------|------|
| 구자빈 (팀장) | 20203028 | 슈터 플레이어 & 게임 시스템, 적 AI, 오디오, 맵 & 렌더링 |
| 전경진 | 20203129 | 서포터 플레이어 & 건설 시스템, 정화 시스템, 네트워크 |

---

## 4. 사용법

### 빌드 다운로드 (권장)

소스 코드 빌드 없이 바로 실행할 수 있는 Windows 빌드 파일을 제공합니다.

[![Download](https://img.shields.io/badge/Download-Latest_Build-blue?style=for-the-badge&logo=github)](https://github.com/kookmin-sw/2026-capstone-12/releases/latest)

1. 위 버튼에서 최신 빌드 zip 파일 다운로드
2. 압축 해제 후 `KMU_CoopDefense.exe` 실행
3. 방 생성 또는 참가 후 슈터 / 서포터 역할 선택

> Windows 10 이상 환경에서 실행을 권장합니다.

---

### 소스 빌드 (개발자용)

#### 실행 환경

- Unity 2022.3.62f3
- Photon PUN 2 (패키지 포함)

#### 에셋 설치

프로젝트에는 용량 문제로 일부 에셋이 포함되어 있지 않습니다.  
아래 링크에서 다운로드 후 `Assets/` 경로에 배치하세요.

**에셋 다운로드**: [Google Drive](https://drive.google.com/drive/folders/1Srj95mDoe_thJmNP6JVgQyLZSnRQ4EBt?usp=drive_link)

```
압축 해제 위치: Assets/

상세:
Z_Assets 폴더째로 Assets 아래에 넣을 것
0506Assets 등의 zip 파일도 압축 해제 후 Z_Assets 내부로 옮길 것
.meta 파일 포함
```

#### 실행 방법

1. Unity Hub에서 프로젝트 열기
2. `Assets/Z_Assets` 에셋 배치 확인
3. `Assets/Project/Scenes/MainMenuScene` 씬 실행
4. 방 생성 또는 참가 후 슈터 / 서포터 역할 선택

---

## 5. 기타

- 개발 기간: 2026.02 ~ 2026.06
- 소속: 국민대학교 소프트웨어학부 캡스톤디자인
