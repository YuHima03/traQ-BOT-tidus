using Stamp = (System.Guid Id, string Name);

namespace BotTidus.Constants
{
    static class TraqStamps
    {
        public static readonly Stamp Clap = (Guid.Parse("027fa329-1b7c-41c3-8b80-665facbdf4aa"), "clap");
        public static readonly Stamp OmaeoKorosu = (Guid.Parse("0190f3ca-05ef-73fa-8892-fe715141f701"), "omaeo_korosu");
        public static readonly Stamp Otsukare = (Guid.Parse("19eb80ae-0467-4409-ad21-5dc5d0148fd6"), "otsukare");
        public static readonly Stamp Wave = (Guid.Parse("54e37bdc-7f8d-4fe9-aaf8-6173b97d0607"), "wave");
    }
}
