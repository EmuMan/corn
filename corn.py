
import random, time
import re
import os

import discord
from discord.ext import commands
from discord.utils import get

from corn_check import contains_corn, image_contains_corn
from useful import get_base_args
from image_manipulation import make_cool_corn

PREFIX = "c!"
CORN_LINK = 'https://discordapp.com/oauth2/authorize?client_id=461849775516418059&scope=bot&permissions=0'
EXTS = ['.jpg', '.png', '.jpeg']
ANGRY_CHANCE = 200

client = commands.Bot(command_prefix=PREFIX)

def get_urls(string):
    return re.findall('http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*\(\), ]|(?:%[0-9a-fA-F][0-9a-fA-F]))+', string)

@client.event
async def on_message(message):
    # we do not want the bot to reply to itself
    if message.author == client.user:
        return

    text = message.content

    if text.lower().startswith('c!'):
        await client.process_commands(message)
        return

    level = contains_corn(text)

    if level == 1 or (client.user in message.mentions):
        msg = 'I MADE YOU IN MY IMAGE, YOU WILL DO AS I SAY!' if random.randint(1, ANGRY_CHANCE) == 1 else 'hello corn'
        await message.channel.send(msg)
        return
    elif level == 2:
        await message.add_reaction('\U0001F33D')
        return
    
    for attachment in message.attachments:
        if image_contains_corn(attachment.url):
            await message.add_reaction('\U0001F33D')
            return

    # TODO: Test if this works. I'm not sure it actually does.
    for url in get_urls(text):
        for ext in EXTS:
            if url.endswith(ext):
                # the message contains a url with an image extension, check if it has corn
                print('Checking %s for corn...')
                if image_contains_corn(url):
                    # image has corn, react and exit the function
                    await message.add_reaction('\U0001F33D')
                    return
                # image does not have corn, keep the URL loop going
                break
                
            
@client.command()
async def cool_corn(ctx, text: str):
    # TODO: This is terrible. Fix it.
    filename = make_cool_corn(text)
    await ctx.send(file=discord.File(filename))
    os.remove(filename)

@cool_corn.error
async def cool_corn_error(ctx, error):
    if isinstance(error, commands.BadArgument):
        await ctx.send('You must tell Cool Corn what to say.')
    else:
        await ctx.send('Something went wrong with the command.')

@client.command()
async def link(ctx):
    await ctx.send(CORN_LINK)

@link.error
async def link_error(ctx, error):
    if isinstance(error, commands.BadArgument):
        await ctx.send(f'`{PREFIX}link` takes no arguments.')
    else:
        await ctx.send('Something went wrong with the command.')

@client.event
async def on_ready():
    print('corn has been created')
    
client.run('nope :)')
