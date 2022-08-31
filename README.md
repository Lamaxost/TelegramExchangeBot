# TelegramEchange

This is the code of Telegram bot, where users can exchange video files.
This bot uses telegram SuperGroups to contain video. There are restriction, only 1 video per 3.2 seconds in group. So you can create some of them and speed up the bot.
There are comfortable translate opportunities, all texts contains in textsConfig.json, now there are RUS language.


The customer of bot used it for TERRIBLE things, but you can exchange funny TikToks


## How to run
If you wanna run bot, you must have create appsettings.json with this properties
```
{
  "ConnectionStrings": {
    "DefaultConnection": "{Db connection string for SQLITE}"
  },
  "BotToken": "{BotToken}",
  "StorageChatIds": [
    {Buffers/storages chat ids}
  ],
  "AdminChatIds": [
    {Chat Id of Admins}
  ]
}
```
## Admin commands
### Message(chatId)
`/message 88005553535` after this command bot asks you for a message, and sents it to user
`/message -1` message will be sended for all users
### Ban (chatId)
`/ban 88005553535` blocks user acces to bot
### Unban (chatId)
`/unban 88005553535` return user acces to bot
### Delete Videos (chatId)
`/delete 88005553535` delete all videos from user in storage and from db
### Give (chatId) (count)
`/delete 88005553535 100` it gives this count of videos to user, but, user need to enter /startexchange and /endechange.