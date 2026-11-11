using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace fptu_cup.Dtos
{
    /// <summary>Mô hình EDM cho route /odata: mỗi entity set là một DTO chỉ-đọc (xem /odata/$metadata).</summary>
    public static class ODataModel
    {
        public static IEdmModel Build()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TournamentDto>("Tournaments");
            builder.EntitySet<TeamDto>("Teams");
            builder.EntitySet<PlayerDto>("Players");
            builder.EntitySet<MatchDto>("Matches");
            builder.EntitySet<VenueDto>("Venues");
            return builder.GetEdmModel();
        }
    }
}
