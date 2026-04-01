using System.Text.Json;

namespace SeinServices.Api.Models.Chungyak.External
{
    /// <summary>
    /// MyHomeRecruitDto 관련 기능을 제공합니다.
    /// </summary>
    public class MyHomeRecruitDto
    {
        public MyHomeRecruitResponse? response { get; set; }
    }

    /// <summary>
    /// MyHomeRecruitResponse 관련 기능을 제공합니다.
    /// </summary>
    public class MyHomeRecruitResponse
    {
        public MyHomeRecruitHeader? header { get; set; }
        public MyHomeRecruitBody? body { get; set; }
    }

    /// <summary>
    /// MyHomeRecruitHeader 관련 기능을 제공합니다.
    /// </summary>
    public class MyHomeRecruitHeader
    {
        public string? resultCode { get; set; }
        public string? resultMsg { get; set; }
    }

    /// <summary>
    /// MyHomeRecruitBody 관련 기능을 제공합니다.
    /// </summary>
    public class MyHomeRecruitBody
    {
        public string? totalCount { get; set; }
        public string? numOfRows { get; set; }
        public string? pageNo { get; set; }
        public List<MyHomeRecruitItem>? item { get; set; }
    }

    /// <summary>
    /// MyHomeRecruitItem 관련 기능을 제공합니다.
    /// </summary>
    public class MyHomeRecruitItem
    {
        public string pblancId { get; set; } = string.Empty;
        public int houseSn { get; set; }
        public string? sttusNm { get; set; }
        public string? pblancNm { get; set; }
        public string? suplyInsttNm { get; set; }
        public string? houseTyNm { get; set; }
        public string? suplyTyNm { get; set; }
        public string? beforePblancId { get; set; }
        public string? rcritPblancDe { get; set; }
        public string? przwnerPresnatnDe { get; set; }
        public string? suplyHoCo { get; set; }
        public string? refrnc { get; set; }
        public string? url { get; set; }
        public string? pcUrl { get; set; }
        public string? mobileUrl { get; set; }
        public string? hsmpNm { get; set; }
        public string? brtcNm { get; set; }
        public string? signguNm { get; set; }
        public string? fullAdres { get; set; }
        public string? rnCodeNm { get; set; }
        public string? refrnLegaldongNm { get; set; }
        public string? pnu { get; set; }
        public string? heatMthdNm { get; set; }
        public JsonElement? totHshldCo { get; set; }
        public int sumSuplyCo { get; set; }
        public int rentGtn { get; set; }
        public int enty { get; set; }
        public int prtpay { get; set; }
        public int surlus { get; set; }
        public int mtRntchrg { get; set; }
        public string? beginDe { get; set; }
        public string? endDe { get; set; }
    }
}

