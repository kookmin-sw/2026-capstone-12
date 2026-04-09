from fpdf import FPDF

class PDF(FPDF):
    def __init__(self):
        super().__init__()
        self.add_font("malgun", "", "C:/Windows/Fonts/malgun.ttf")
        self.add_font("malgun", "B", "C:/Windows/Fonts/malgunbd.ttf")

    def header(self):
        self.set_font("malgun", "B", 10)
        self.set_text_color(150, 150, 150)
        self.cell(0, 8, "KMU Coop Defense - Game Design Document", align="R", new_x="LMARGIN", new_y="NEXT")
        self.line(10, self.get_y(), 200, self.get_y())
        self.ln(4)

    def footer(self):
        self.set_y(-15)
        self.set_font("malgun", "", 8)
        self.set_text_color(150, 150, 150)
        self.cell(0, 10, f"- {self.page_no()} -", align="C")

    def title_section(self, text):
        self.set_font("malgun", "B", 18)
        self.set_text_color(30, 30, 30)
        self.cell(0, 14, text, new_x="LMARGIN", new_y="NEXT")
        self.set_draw_color(50, 120, 200)
        self.set_line_width(0.8)
        self.line(10, self.get_y(), 200, self.get_y())
        self.ln(6)

    def sub_section(self, text):
        self.set_font("malgun", "B", 13)
        self.set_text_color(50, 50, 50)
        self.cell(0, 10, text, new_x="LMARGIN", new_y="NEXT")
        self.ln(2)

    def body(self, text):
        self.set_font("malgun", "", 10)
        self.set_text_color(40, 40, 40)
        self.multi_cell(0, 7, text)
        self.ln(2)

    def bullet(self, text, indent=10):
        self.set_font("malgun", "", 10)
        self.set_text_color(40, 40, 40)
        self.set_x(self.l_margin + indent)
        w = self.w - self.l_margin - self.r_margin - indent
        self.multi_cell(w, 7, f"•  {text}")
        self.ln(1)

    def numbered(self, num, text, indent=10):
        self.set_font("malgun", "", 10)
        self.set_text_color(40, 40, 40)
        self.set_x(self.l_margin + indent)
        w = self.w - self.l_margin - self.r_margin - indent
        self.multi_cell(w, 7, f"{num}.  {text}")
        self.ln(1)


pdf = PDF()
pdf.set_auto_page_break(auto=True, margin=20)

# ===== Page 1: Title page =====
pdf.add_page()
pdf.ln(50)
pdf.set_font("malgun", "B", 32)
pdf.set_text_color(30, 30, 30)
pdf.cell(0, 20, "KMU Coop Defense", align="C", new_x="LMARGIN", new_y="NEXT")
pdf.set_font("malgun", "", 16)
pdf.set_text_color(80, 80, 80)
pdf.cell(0, 12, "게임 기획 변경안", align="C", new_x="LMARGIN", new_y="NEXT")
pdf.ln(10)
pdf.set_draw_color(50, 120, 200)
pdf.set_line_width(1)
pdf.line(60, pdf.get_y(), 150, pdf.get_y())
pdf.ln(10)
pdf.set_font("malgun", "", 11)
pdf.set_text_color(100, 100, 100)
pdf.cell(0, 8, "2인 협동 타워 디펜스 | Unity + Photon PUN2", align="C", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 8, "2026-04-06", align="C", new_x="LMARGIN", new_y="NEXT")

# ===== Page 2: Overview =====
pdf.add_page()

pdf.title_section("1. 개요")
pdf.body(
    "기존 웨이브 기반 타워 디펜스 시스템을 폐지하고, "
    "맵 탐험 + 구역 공략 방식으로 게임 흐름을 전면 변경한다. "
    "플레이어(Shooter + Supporter)가 맵을 돌아다니며 3개 구역의 좀비타워를 "
    "순서대로 파괴하면 게임 클리어."
)

pdf.title_section("2. 맵 구조")

pdf.sub_section("2.1 전체 맵")
pdf.bullet("큰 사각형 형태의 단일 맵")
pdf.bullet("플레이어 시작 위치: 7시 방향")
pdf.bullet("3개 구역이 5시, 10시, 1시 방향에 배치")
pdf.bullet("구역 외 필드: 자유 이동 가능 영역, 랜덤 좀비 출현")

pdf.sub_section("2.2 구역 (Zone)")
pdf.bullet("각 구역은 잠겨 있으며, 내부에 좀비타워 1개 + 다수의 좀비 배치")
pdf.bullet("플레이어가 구역 입구에서 상호작용하면 문이 열림")
pdf.bullet("문이 열리면 내부 좀비가 한꺼번에 쏟아져 나옴")
pdf.bullet("구역 해금 순서는 매 게임마다 랜덤으로 결정됨")

pdf.sub_section("2.3 좀비타워")
pdf.bullet("각 구역 내부에 위치한 파괴 대상 구조물")
pdf.bullet("좀비타워 자체는 좀비를 스폰하지 않음 (구역 내 좀비는 미리 배치)")
pdf.bullet("파괴 시: 다음 구역 해금 + 전체 좀비 강화 트리거")

# ===== Page 3: Game Flow =====
pdf.add_page()

pdf.title_section("3. 게임 진행 흐름")

pdf.numbered(1, "게임 시작 → 3개 구역 중 랜덤으로 첫 번째 해금 대상 결정")
pdf.numbered(2, "플레이어가 해금된 구역 입구로 이동 → 상호작용으로 문 열기")
pdf.numbered(3, "구역 내 좀비 쏟아짐 → 전투 진행")
pdf.numbered(4, "좀비타워 파괴 → 좀비 전체 강화 + 다음 구역 해금")
pdf.numbered(5, "2~4 반복 (총 3회)")
pdf.numbered(6, "마지막(3번째) 좀비타워 파괴 → 게임 클리어")
pdf.ln(4)

pdf.body(
    "※ 구역 공략 중에도 필드에서는 일정 시간마다 좀비가 랜덤 스폰되므로, "
    "서포터의 건물 배치로 거점을 방어하는 것이 중요하다."
)

pdf.title_section("4. 좀비 시스템")

pdf.sub_section("4.1 AI 변경")
pdf.bullet("기존: 스폰 즉시 플레이어를 무조건 추적")
pdf.bullet("변경: 어그로 범위(Aggro Range) 도입 — 일정 거리 안에 플레이어가 들어와야 추적 시작")
pdf.bullet("구역 내 좀비: 문이 열리면 활성화되어 밖으로 쏟아져 나옴")

pdf.sub_section("4.2 필드 스폰")
pdf.bullet("구역 외 맵 전역에서 일정 시간 간격으로 랜덤 위치에 좀비 스폰")
pdf.bullet("기존 웨이브 시스템을 대체하는 지속적 위협 요소")

pdf.sub_section("4.3 좀비 강화")
pdf.bullet("좀비타워를 파괴할 때마다 모든 좀비(기존 + 이후 스폰)가 강화됨")
pdf.bullet("강화 요소 예시: 체력 증가, 공격력 증가, 이동속도 증가 등")
pdf.bullet("3단계 강화 (타워 1개 파괴 → 2개 파괴 → 3개 파괴 시점)")

# ===== Page 4: Shooter Leveling =====
pdf.add_page()

pdf.title_section("5. 슈터 레벨업 시스템 (신규)")

pdf.sub_section("5.1 목적")
pdf.body(
    "서포터는 건물 배치/수리/판매 등 할 일이 많은 반면, "
    "슈터는 단순 사격만 반복하는 문제가 있었다. "
    "레벨업 시스템을 추가하여 슈터의 성장감과 플레이 요소를 보강한다."
)

pdf.sub_section("5.2 경험치 획득")
pdf.bullet("좀비 처치 시 경험치 획득")
pdf.bullet("좀비타워 파괴 시 대량 경험치 획득")

pdf.sub_section("5.3 레벨업 & 포인트")
pdf.bullet("경험치가 일정량 누적되면 레벨업")
pdf.bullet("레벨업 시 스탯 포인트 획득")

pdf.sub_section("5.4 강화 가능 스탯")
pdf.bullet("총 데미지 (Damage) — 사격 공격력 증가")
pdf.bullet("체력 (HP) — 최대 체력 증가")
pdf.bullet("이동속도 (Move Speed) — 캐릭터 이동속도 증가")

pdf.title_section("6. 기존 시스템 변경사항")

pdf.sub_section("삭제")
pdf.bullet("웨이브 시스템 (WaveManager) — 준비시간/웨이브 사이클 제거")
pdf.bullet("웨이브 기반 승리 조건 — 좀비타워 전파괴로 대체")

pdf.sub_section("유지")
pdf.bullet("서포터 건물 배치 시스템 (BuildSystem, GridManager 등)")
pdf.bullet("Photon PUN2 네트워크 (마스터 클라이언트 권위 모델)")
pdf.bullet("역할 분리: Shooter (1인칭 FPS) + Supporter (탑뷰)")
pdf.bullet("기존 좀비 타입 (Basic, Tank, Fast)")

pdf.sub_section("변경")
pdf.bullet("적 AI: 무조건 추적 → 어그로 범위 기반 추적")
pdf.bullet("적 스폰: 웨이브 → 필드 랜덤 + 구역 내 사전 배치")
pdf.bullet("승리 조건: 5웨이브 생존 → 좀비타워 3개 전파괴")

# ===== Page 5: Summary diagram =====
pdf.add_page()

pdf.title_section("7. 맵 배치 개념도")
pdf.ln(5)

# Draw a simple map diagram
cx, cy = 105, 140  # center of map
size = 70  # half-size of square

# Map border
pdf.set_draw_color(80, 80, 80)
pdf.set_line_width(0.5)
pdf.rect(cx - size, cy - size, size * 2, size * 2)

# Label
pdf.set_font("malgun", "B", 9)
pdf.set_text_color(80, 80, 80)
pdf.set_xy(cx - size, cy - size - 8)
pdf.cell(size * 2, 6, "[ MAP ]", align="C")

# Zone positions (clock positions on a square map)
import math
radius = 55

zones = [
    ("5시 구역", 5, (200, 80, 80)),
    ("10시 구역", 10, (80, 160, 80)),
    ("1시 구역", 1, (80, 80, 200)),
]

# Clock position to angle (12 o'clock = 90 deg up, clockwise)
def clock_to_xy(hour, r):
    angle = math.radians(90 - hour * 30)
    return cx + r * math.cos(angle), cy - r * math.sin(angle)

# Player start at 7 o'clock
px, py = clock_to_xy(7, radius)
pdf.set_fill_color(50, 180, 50)
pdf.circle(px, py, 6, style="F")
pdf.set_font("malgun", "B", 8)
pdf.set_text_color(255, 255, 255)
pdf.set_xy(px - 10, py - 3)
pdf.cell(20, 6, "START", align="C")

pdf.set_font("malgun", "B", 8)
pdf.set_text_color(50, 180, 50)
pdf.set_xy(px - 15, py + 5)
pdf.cell(30, 6, "플레이어 (7시)", align="C")

# Draw zones
for name, hour, color in zones:
    zx, zy = clock_to_xy(hour, radius)
    pdf.set_fill_color(*color)
    pdf.set_draw_color(*color)
    pdf.rect(zx - 12, zy - 8, 24, 16, style="F")
    pdf.set_font("malgun", "B", 7)
    pdf.set_text_color(255, 255, 255)
    pdf.set_xy(zx - 12, zy - 4)
    pdf.cell(24, 8, name, align="C")

# Center label
pdf.set_font("malgun", "", 8)
pdf.set_text_color(120, 120, 120)
pdf.set_xy(cx - 20, cy - 3)
pdf.cell(40, 6, "필드 영역", align="C")
pdf.set_xy(cx - 25, cy + 3)
pdf.cell(50, 6, "(랜덤 좀비 스폰)", align="C")

# Legend
pdf.set_xy(10, cy + size + 15)
pdf.set_font("malgun", "B", 10)
pdf.set_text_color(30, 30, 30)
pdf.cell(0, 8, "범례", new_x="LMARGIN", new_y="NEXT")

pdf.set_font("malgun", "", 9)
pdf.set_text_color(50, 50, 50)

pdf.set_fill_color(50, 180, 50)
pdf.circle(16, pdf.get_y() + 3, 3, style="F")
pdf.set_x(22)
pdf.cell(0, 7, "플레이어 시작 위치 (7시)", new_x="LMARGIN", new_y="NEXT")

for name, _, color in zones:
    pdf.set_fill_color(*color)
    pdf.rect(13, pdf.get_y(), 6, 6, style="F")
    pdf.set_x(22)
    pdf.cell(0, 7, f"{name} (좀비타워 + 좀비 다수 배치)", new_x="LMARGIN", new_y="NEXT")

pdf.ln(3)
pdf.set_font("malgun", "", 9)
pdf.set_text_color(100, 100, 100)
pdf.body("※ 구역 해금 순서는 매 게임마다 랜덤으로 결정됩니다.")

# Save
output_path = "C:/Users/PC1/kmu-coop-defense/GameDesign_v2.pdf"
pdf.output(output_path)
print(f"PDF saved to: {output_path}")
