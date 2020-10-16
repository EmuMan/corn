from clarifai.rest import ClarifaiApp

api_key = ''

app = ClarifaiApp(api_key=api_key)

model = app.public_models.general_model

punctuation = '~-_ .,/\\|=+'

def contains_corn(string):
    string = string.lower()
    corn = 'corn'

    corn_iter = 0

    for char in string:
        if char == corn[corn_iter]:
            if corn_iter == len(corn) - 1:
                return 1
            else:
                corn_iter += 1
        elif (char in punctuation) or (corn_iter > 0 and char == corn[corn_iter - 1]):
            continue
        else:
            corn_iter = 0

    corn_iter = 0

    for char in string:
        if char == corn[corn_iter]:
            if corn_iter == len(corn) - 1:
                return 2
            else:
                corn_iter += 1
    
    return 0

def image_contains_corn(link):
    try:
        response = model.predict_by_url(url=link)

        data = response['outputs'][0]['data']

        top_10 = [concept['name'] for concept in data['concepts']][:10]

        print(top_10)
        
        if 'corn' in top_10 or 'corncob' in top_10:
            return True
        return False
    except:
        return False
