namespace apiBask.Models.Response
{
    public class Response
    {
        public int exito { get; set; }

        public string mensaje { get; set; }

        public object data { get; set; }

        public Response()
        {
            exito = 0;
        }
    }
}
