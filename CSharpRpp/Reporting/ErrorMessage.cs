namespace CSharpRpp.Reporting
{
    public class ErrorMessage
    {
        public int Code { get; set; }
        public string Message { get; set; }

        public ErrorMessage(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString()
        {
            return $"error RP{Code}: {Message}";
        }
    }
}