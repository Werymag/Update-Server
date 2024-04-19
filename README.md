<h1 align="center">Сервер обновление приложений и скачивания установочных файлов</h1>

Простой сервер для автоматической загрузки и последущего обновление десктопных приложений. 

Сервер состоит из двух частей:
- Web Api для работы с файлами;
- Веб страницы для скачивания установочных файлов или загрузки актуальной версии вручную.



Описание Api серера для доступа к файлам версий программ.

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
    "program":"Program Name 1",
    "Version":"1.0.0.2"
  },
  {
    "program":"Program Name 2",
    "version":"3.2.0.13"
  }
]
```



#### ⬤ Получить список доступых версий для программы:
```
http://{serverulr}/Version/GetVersions
    ? program={имя программы}
```
Ответ возвращается в формате JSON.

Содержание ответа: наименование программы и информация о доступных для скачивания версиях.
Информация о версиях представляет из себя номер версии и список изменений
```
{
  "program":"Program Name 1",
  "versions":
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


#### ⬤ Получить список доступых версий для программы:
```
http://{serverulr}/Version/GetActualVersion
    ? program={имя программы}
```
Ответ возвращается в формате JSON.

Содержание ответа: номер актуальной версии программы.
```
1.3.10.0
```



#### ⬤ Получить список доступых версий для программы:
```
http://{serverulr}/Version/GetFilesListWithHash
    ? program={имя программы}
    & version={номер версий}
```
Ответ возвращается в формате JSON.

Содержание ответа: список файлов приложения с MD5 хешем в виде строки
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

#### ⬤ Загрузить файл:
```
http://{serverulr}/Version/GetFile
    ? Program={имя программы}
    & Version={номер версий}
    & FilePath={оносительный путь к файлу}
```
Ответ возвращается в формате бинарного файла.

docker clone {}
docker build -t updateserver .
docker run -p 8888:80  -e login={login} -e password={password} -v {pathtoprograms}:/app/programs  --name updateserver updateserver
