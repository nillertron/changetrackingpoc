namespace ChangeTrackingPoc.Model
{
    class ChangeTable
    {
        public int Sys_Change_Version { get; set; }
        public Cud Operation { get; set; }
        public string Id { get; set; }
    }
}
