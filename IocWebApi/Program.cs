using FastIOC.Builder;
using FrionGraet;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region ���Դ���

IocService.RegistService(typeof(ICustomJob));//1 �����Ӧ�Ľӿ�
var h = IocService.GetSingleton<ICustomJob>("Test");//2 �������ƻ�ȡ��Ӧ�Ľӿ�
h.HelloWord();//3 ���÷���
#endregion


builder.Services.AddSingleton(new AppSettings(builder.Configuration));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.Run();