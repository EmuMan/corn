import textwrap
import random
import numpy
import os
from PIL import Image
from PIL import ImageFont
from PIL import ImageDraw

cool_corn = Image.open('cool_corn.png')

def find_coeffs(pa, pb):
    matrix = []
    for p1, p2 in zip(pa, pb):
        matrix.append([p1[0], p1[1], 1, 0, 0, 0, -p2[0]*p1[0], -p2[0]*p1[1]])
        matrix.append([0, 0, 0, p1[0], p1[1], 1, -p2[1]*p1[0], -p2[1]*p1[1]])

    A = numpy.matrix(matrix, dtype=numpy.float)
    B = numpy.array(pb).reshape(8)

    res = numpy.dot(numpy.linalg.inv(A.T * A) * A.T, B)
    return numpy.array(res).reshape(8)

def text_over_image(image, text, font_size, line_length):
    font = ImageFont.truetype('Consolas.ttf', font_size)
    text_lines = []
    for line in text.split('\n'):
        [text_lines.append(l) for l in textwrap.wrap(line, width=line_length)]
    new_img = Image.new('RGBA', (image.width, image.height + font_size * len(text_lines)), (255,255,255,255))
    new_img.paste(image, (0, font_size * len(text_lines)))
    draw = ImageDraw.Draw(new_img)
    draw.text((0,0), '\n'.join(text_lines), (0,0,0), font=font)
    return new_img

def overlay_image(image, overlay, quad):
    overlay_a = overlay.convert('RGBA')
    x_min = min([c[0] for c in quad])
    y_min = min([c[1] for c in quad])
    new_quad = [(x - x_min, y - y_min) for x, y in quad]
    x_max = max([c[0] for c in new_quad])
    y_max = max([c[0] for c in new_quad])
    new_x_min = min([c[0] for c in new_quad])
    new_y_min = min([c[0] for c in new_quad])
    coeffs = find_coeffs(new_quad, [(0,0), (overlay_a.width, 0), (overlay_a.width, overlay_a.height), (0, overlay_a.height)])
    overlay_warped = overlay_a.transform((x_max, y_max), Image.PERSPECTIVE, coeffs, Image.BICUBIC)
    image.paste(overlay_warped, (new_x_min, new_y_min), overlay_warped)

def make_cool_corn(text):
    cc = text_over_image(cool_corn, text.replace('\\n', '\n'), 55, 41)
    while(1):
        filename = "%d.png" % random.randint(0, 99999)
        if not os.path.isfile(filename):
            cc.save(filename)
            return filename
    
