# uhfManage
基于RFID的病人管理系统，实验室项目初级版本

# 系统包括以下功能：

1.当病患入院时，医院为其配发RFID标签，在护士站电脑终端为其建立档案，并将RFID标签与该患者信息绑定；

2.护士站进行信息管理与维护，例如记录病患生理参数历史数据、个人信息更新、历史用药记录等等；

3.当医生或护士查房时，使用手持机靠近某一患者扫描其RFID标签，手持机获取RFID标签信息并通过局域网将信息发送至护士站电脑终端，护士站电脑终端调取数据库匹配标签信息，将匹配信息通过局域网发送至手持机，手持机收到信息之后将其显示在软件界面上，医生或护士可以查看该患者的相关信息；

4.亲属可以通过手机APP程序查看患者信息，例如医嘱、用药记录等。
# 开发软件

手机APP开发软件：Android Studio 3.0.1+ Gradle-3.5.1+ maxSdkVersion 21+ minSdkVersion 5

监护中心软件开发软件：Visual Studio 2017