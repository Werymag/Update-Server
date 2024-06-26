<h1 align="center">Сервер обновление приложений и скачивания установочных файлов</h1>

**Простой сервер для автоматической загрузки и последующего обновления десктопных приложений.** 


## Процедура запуска сервера с использованием docker

```
git clone https://github.com/Werymag/Update-Server.git
```
```
docker build -t updateserver Update-Server
```
```
docker run -p {serverport}:80  -e login={login} -e password={password} -v {pathtofiles}:/app/programs  --name updateserver updateserver
```

Где:
- serverport - порт по которому будет доступен сайт и сервер API;
- login, password - логин и пароль для доступа к функцоналу загрузки и удаление версий;
- pathtofiles - место хранения файлов программ;
  



Сервер состоит из двух частей:
- Web Api для работы с файлами;
- Веб страницы для скачивания установочных файлов или загрузки актуальной версии вручную.



## Описание Api сервера для доступа к файлам версий программ.

#### ⬤  Получить список доступных для скачивания программ:
```
http://{serverulr}/Version/GetPrograms
```
Ответ возвращается в формате JSON.

Содержание ответа: список программ с номером актуальной версии. 

Пример ответа:
```
[
  {
    "Program":"Program Name 1",
    "Version":"1.0.0.2"
  },
  {
    "Program":"Program Name 2",
    "Version":"3.2.0.13"
  }
]
```



#### ⬤ Получить список доступных версий для программы:
```
http://{serverulr}/Version/GetVersions
    ? Program={имя программы}
```
Ответ возвращается в формате JSON.

Содержание ответа: наименование программы и информация о доступных для скачивания версиях.
Информация о версиях представляет из себя номер версии и список изменений
Пример ответа:
```
{
  "Program":"Program Name 1",
  "Versions":
    [
      {
        "Version":"1.0.0.15",
        "Changelog":"Исправление критических багов"       
      },
      {
        "Version":"1.1.0.0",
        "Changelog":"Добавлена возможность автоматического обновления программы при запуске"       
      }
    ]
}
```


#### ⬤ Получить актуальную версию программы
```
http://{serverulr}/Version/GetActualVersion
    ? Program={имя программы}
```
Ответ возвращается в формате JSON.

Содержание ответа: номер актуальной версии программы.
Пример ответа:
```
1.3.10.0
```



#### ⬤ Получить список файлов программы с MD5 хешем:
```
http://{serverulr}/Version/GetFilesListWithHash
    ? Program={имя программы}
    & Version={номер версий}
```
Ответ возвращается в формате JSON.

Содержание ответа: список файлов приложения с MD5 хешем в виде строки.
Пример ответа:
```
[
  {"FileName":"\\Accessibility.dll","Md5Hash":"8c2bceb862e7d6fc370f9b8f0941d67e"},
  {"FileName":"\\Calculate_reports.dll","Md5Hash":"8c3d1795025ee478c457ffc43f512799"},
  {"FileName":"\\Calculate_reports.pdb","Md5Hash":"a223e0e5e2b9c7e4adbdfe84b5294429"},
  {"FileName":"\\ClosedXML.dll","Md5Hash":"2023ac70abbf3843869f671f98c99d1b"},
  {"FileName":"\\clretwrc.dll","Md5Hash":"9e4d24c879d87b2f1470e76f876b5e26"},
  ...
]
```

#### ⬤ Скачать обновляемый файл программы:
```
http://{serverulr}/Version/GetFile
    ? Program={имя программы}
    & Version={номер версий}
    & FilePath={относительный путь к файлу}
```
Ответ возвращается в формате бинарного файла.


#### ⬤ Скачать установочный файл программы:
```
http://{serverulr}/Version/GetInstallFile
    ? Program={имя программы}
    & Version={номер версий}
```
Ответ возвращается в формате установочного файла программы.

Сайт представляет из себя странцу для скачивания программ. Загрузка и удаление версий программы возможна только полсе авторизации данными указанными при запуске контейнера:

<img src="https://github.com/Werymag/Update-Server/assets/60127528/81720e5a-5ce2-4a6a-9390-a28820397e86" height="170"> <img src="https://github.com/Werymag/Update-Server/assets/60127528/8e9e86f2-e878-4fc6-9b46-445aced82454" height="170"> <img src="https://github.com/Werymag/Update-Server/assets/60127528/2f601592-fc60-4478-bdf4-9d9d02dca9ed" height="170">


## В каталоге sripts приведен примеры скриптов для автоматической публикации, сборки установочного файла и последующей отправки на сервере декстопного приложения. 
