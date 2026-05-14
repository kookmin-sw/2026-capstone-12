public enum GameSoundType
{
    None = -1, // 비활성 타입

    // 핑 알림 사운드
    PingNormal = 0,
    PingDanger = 1,
    PingHelp = 2,

    // 월드 위치 게임 사운드
    BuildStructure = 10,
    SupplyItem = 11,
    GetItem = 12,
    TurretAttack = 13, // 터렛 발사 사운드
    BuildingDestroyed = 14, // 건물 파괴 사운드
    SpawnCoreDestroyed = 15, // 스폰 코어 파괴 사운드
    SlowTowerActivated = 16, // SlowTower 설치 활성화 사운드
    LightPylonActivated = 17, // LightPylon 설치 활성화 사운드
    PurificationBeaconActivated = 18, // Purification Beacon 설치 활성화 사운드
    SpawnCoreOrbBreak = 19,

    // 전역 알림 사운드
    ShooterDeath = 20,
    ShooterRespawn = 21,
    ShooterFire = 22, // Shooter 발사 사운드
    ShooterReload = 23, // Shooter 재장전 사운드
    ShooterWalk = 24, // Shooter 걷기 사운드
    ShooterRun = 25, // Shooter 달리기 사운드

    // 로컬 효과 적용 사운드
    ApplyHealthPack = 30,
    ApplyAmmoPack = 31,

    // 몬스터 및 구조물 위치 사운드
    BasicFastEnemyGrowl = 40, // Basic/Fast Enemy 울음 사운드
    TankEnemyGrowl = 41, // Tank Enemy 울음 사운드
    StructureAttacked = 50, // 구조물 피격 사운드
}
