using System.ComponentModel.DataAnnotations;
using EmailServiceAPI.Models;

namespace EmailServiceAPI.Tests.Models;

public class ContactFormRequestTests
{
    private static ContactFormRequest CreateValidRequest() => new()
    {
        FirstName = "John",
        SurName = "Doe",
        Email = "john@example.com",
        QueryType = "general",
        Message = "Hello, this is a test message."
    };

    private static List<ValidationResult> ValidateModel(ContactFormRequest model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Valid_Request_Has_No_Validation_Errors()
    {
        var request = CreateValidRequest();
        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Fact]
    public void FirstName_Is_Required()
    {
        var request = CreateValidRequest();
        request.FirstName = string.Empty;
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("FirstName"));
    }

    [Fact]
    public void FirstName_Exceeds_MaxLength_Fails()
    {
        var request = CreateValidRequest();
        request.FirstName = new string('a', 101);
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("FirstName"));
    }

    [Fact]
    public void SurName_Is_Required()
    {
        var request = CreateValidRequest();
        request.SurName = string.Empty;
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("SurName"));
    }

    [Fact]
    public void SurName_Exceeds_MaxLength_Fails()
    {
        var request = CreateValidRequest();
        request.SurName = new string('a', 101);
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("SurName"));
    }

    [Fact]
    public void Email_Is_Required()
    {
        var request = CreateValidRequest();
        request.Email = string.Empty;
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void Email_Invalid_Format_Fails()
    {
        var request = CreateValidRequest();
        request.Email = "not-an-email";
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void Email_Exceeds_MaxLength_Fails()
    {
        var request = CreateValidRequest();
        request.Email = new string('a', 246) + "@test.com";
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void QueryType_Is_Required()
    {
        var request = CreateValidRequest();
        request.QueryType = string.Empty;
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("QueryType"));
    }

    [Fact]
    public void QueryType_General_Is_Valid()
    {
        var request = CreateValidRequest();
        request.QueryType = "general";
        var results = ValidateModel(request);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("QueryType"));
    }

    [Fact]
    public void QueryType_Project_Is_Valid()
    {
        var request = CreateValidRequest();
        request.QueryType = "project";
        var results = ValidateModel(request);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("QueryType"));
    }

    [Fact]
    public void QueryType_Feedback_Is_Valid()
    {
        var request = CreateValidRequest();
        request.QueryType = "feedback";
        var results = ValidateModel(request);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("QueryType"));
    }

    [Fact]
    public void QueryType_Other_Is_Valid()
    {
        var request = CreateValidRequest();
        request.QueryType = "other";
        var results = ValidateModel(request);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("QueryType"));
    }

    [Fact]
    public void QueryType_Invalid_Value_Fails()
    {
        var request = CreateValidRequest();
        request.QueryType = "invalid";
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("QueryType"));
    }

    [Fact]
    public void Message_Is_Required()
    {
        var request = CreateValidRequest();
        request.Message = string.Empty;
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("Message"));
    }

    [Fact]
    public void Message_Exceeds_MaxLength_Fails()
    {
        var request = CreateValidRequest();
        request.Message = new string('a', 5001);
        var results = ValidateModel(request);
        Assert.Contains(results, r => r.MemberNames.Contains("Message"));
    }

    [Fact]
    public void Website_Is_Optional_Honeypot()
    {
        var request = CreateValidRequest();
        request.Website = null;
        var results = ValidateModel(request);
        Assert.Empty(results);
    }
}
