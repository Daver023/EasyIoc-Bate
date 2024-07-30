using FastIOC.Builder;
using FrionGraet;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region 测试代码

IocService.RegistService(typeof(ICustomJob));//1 添加相应的接口
var h = IocService.GetSingleton<ICustomJob>("Test");//2 根据名称获取相应的接口
h.HelloWord();//3 调用方法
#endregion


builder.Services.AddSingleton(new AppSettings(builder.Configuration));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.Run();