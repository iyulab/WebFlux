namespace WebFlux.Core.Models;

/// <summary>
/// README.md 메타데이터
/// </summary>
public class ReadmeMetadata
{
    /// <summary>
    /// README 파일 경로
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// README 내용
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 프로젝트 제목
    /// </summary>
    public string? ProjectTitle { get; set; }

    /// <summary>
    /// 프로젝트 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 설치 지침
    /// </summary>
    public string? InstallationInstructions { get; set; }

    /// <summary>
    /// 사용법
    /// </summary>
    public string? UsageInstructions { get; set; }

    /// <summary>
    /// 라이선스 정보
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// 기여자 정보
    /// </summary>
    public List<string> Contributors { get; set; } = new();

    /// <summary>
    /// 배지 목록
    /// </summary>
    public List<string> Badges { get; set; } = new();
}

/// <summary>
/// _config.yml 메타데이터
/// </summary>
public class ConfigMetadata
{
    /// <summary>
    /// 설정 파일 경로
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 사이트 제목
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 사이트 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 기본 URL
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// 사용 중인 테마
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// 플러그인 목록
    /// </summary>
    public List<string> Plugins { get; set; } = new();

    /// <summary>
    /// 빌드 설정
    /// </summary>
    public Dictionary<string, object> BuildSettings { get; set; } = new();
}

/// <summary>
/// humans.txt 메타데이터
/// </summary>
public class HumansMetadata
{
    /// <summary>
    /// 팀 정보
    /// </summary>
    public List<TeamMember> Team { get; set; } = new();

    /// <summary>
    /// 감사 인사
    /// </summary>
    public List<string> Thanks { get; set; } = new();

    /// <summary>
    /// 사이트 정보
    /// </summary>
    public SiteInfo? Site { get; set; }
}

/// <summary>
/// 팀 멤버 정보
/// </summary>
public class TeamMember
{
    /// <summary>
    /// 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 역할
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 연락처
    /// </summary>
    public string? Contact { get; set; }

    /// <summary>
    /// 위치
    /// </summary>
    public string? Location { get; set; }
}

/// <summary>
/// 사이트 정보
/// </summary>
public class SiteInfo
{
    /// <summary>
    /// 마지막 업데이트
    /// </summary>
    public DateTime? LastUpdate { get; set; }

    /// <summary>
    /// 언어
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// 도구/기술
    /// </summary>
    public List<string> Tools { get; set; } = new();
}

/// <summary>
/// security.txt 메타데이터
/// </summary>
public class SecurityMetadata
{
    /// <summary>
    /// 연락처 정보
    /// </summary>
    public List<string> Contact { get; set; } = new();

    /// <summary>
    /// 암호화 키
    /// </summary>
    public string? Encryption { get; set; }

    /// <summary>
    /// 확인 정보
    /// </summary>
    public string? Acknowledgments { get; set; }

    /// <summary>
    /// 정책 URL
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// 채용 정보
    /// </summary>
    public string? Hiring { get; set; }

    /// <summary>
    /// 만료일
    /// </summary>
    public DateTime? Expires { get; set; }
}

/// <summary>
/// ads.txt 메타데이터
/// </summary>
public class AdsMetadata
{
    /// <summary>
    /// 광고 시스템 도메인
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// 게시자 계정 ID
    /// </summary>
    public string PublisherId { get; set; } = string.Empty;

    /// <summary>
    /// 관계 유형 (DIRECT, RESELLER)
    /// </summary>
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>
    /// 인증 기관 ID
    /// </summary>
    public string? CertificationAuthorityId { get; set; }
}

/// <summary>
/// BingSiteAuth.xml 메타데이터
/// </summary>
public class BingSiteAuthMetadata
{
    /// <summary>
    /// 사용자 정보
    /// </summary>
    public List<BingUser> Users { get; set; } = new();
}

/// <summary>
/// Bing 사용자 정보
/// </summary>
public class BingUser
{
    /// <summary>
    /// 사용자 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 이메일
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// OpenAPI 메타데이터
/// </summary>
public class OpenApiMetadata
{
    /// <summary>
    /// OpenAPI 버전
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// API 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// API 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// API 버전
    /// </summary>
    public string ApiVersion { get; set; } = string.Empty;

    /// <summary>
    /// 서버 목록
    /// </summary>
    public List<OpenApiServer> Servers { get; set; } = new();

    /// <summary>
    /// 경로 목록
    /// </summary>
    public List<string> Paths { get; set; } = new();

    /// <summary>
    /// 태그 목록
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// OpenAPI 서버 정보
/// </summary>
public class OpenApiServer
{
    /// <summary>
    /// 서버 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 서버 설명
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Schema.org 메타데이터
/// </summary>
public class SchemaMetadata
{
    /// <summary>
    /// 스키마 타입
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 제목
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 추가 속성
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// 잘 알려진 메타데이터
/// </summary>
public class WellKnownMetadata
{
    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 파일 타입
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 내용
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 파싱된 데이터
    /// </summary>
    public Dictionary<string, object> ParsedData { get; set; } = new();

    /// <summary>
    /// 마지막 수정 시간
    /// </summary>
    public DateTime? LastModified { get; set; }
}