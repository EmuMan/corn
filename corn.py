import discord
import random, time
import re
import os

from discord.utils import get

from corn_check import contains_corn, image_contains_corn
from useful import get_base_args
from image_manipulation import make_cool_corn

client = discord.Client()

corn_link = 'https://discordapp.com/oauth2/authorize?client_id=461849775516418059&scope=bot&permissions=0'

exts = ['.jpg', '.png', '.jpeg']

def get_urls(string):
    urls = re.findall('http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*\(\), ]|(?:%[0-9a-fA-F][0-9a-fA-F]))+', string) 
    return urls

@client.event
async def on_message(message):
    # we do not want the bot to reply to itself
    if message.author == client.user:
        return

    text = message.content

    if text.lower().startswith('c!'):
        cmd = text[2:]
        base, args = get_base_args(cmd)
        if base == 'link':
            if len(args) == 0:
                await message.channel.send(corn_link)
            else:
                await message.channel.send('Link requests don\'t take any arguments.')
            return
        elif base == 'cool_corn':
            if len(args) == 1:
                filename = make_cool_corn(args[0])
                await message.channel.send(file=discord.File(filename))
                os.remove(filename)
            else:
                await message.channel.send('You must give a text string to caption Cool Corn with.')
            return

    level = contains_corn(text)

    if level == 1 or (client.user in message.mentions) or (u'\U0001F33D' in text):
        print('corn has been summoned on ' + time.asctime())
        if random.randint(1, 200) == 1:
            print('ANGRY CORN ACTIVATE')
            msg = 'I MADE YOU IN MY IMAGE, YOU WILL DO AS I SAY!'
        else:
            msg = 'hello corn'
        await message.channel.send(msg)
        return
    elif level == 2:
        await message.add_reaction('\U0001F33D')
        return
    
    for attachment in message.attachments:
        if image_contains_corn(attachment.url):
            await message.add_reaction('\U0001F33D')
            return

    for url in get_urls(text):
        valid = False
        for ext in exts:
            if url.endswith(ext):
                valid = True
                break
        if valid:
            print('Checking %s for corn...')
            if image_contains_corn(url):
                await message.add_reaction('\U0001F33D')
                return
                
            
                    

@client.event
async def on_ready():
    print('corn has been created')
    
client.run('nope :)')
