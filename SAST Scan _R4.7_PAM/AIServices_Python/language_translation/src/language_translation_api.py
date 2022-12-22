import torch
import time
import re
from flask import Flask, request, jsonify
from flask_cors import CORS
import warnings
from textblob import TextBlob
import json
app = Flask(__name__)
global model_Object_LT
global model_flag_LT
model_flag_LT = False
global model_client_language
model_client_language=None
#global model_client_dest_LT
#model_client_dest_LT=None
# List available models
#model_list=torch.hub.list('pytorch/fairseq')  # [..., 'transformer.wmt16.en-de', ... ]
#print("Model list",model_list)
# Note: WMT'19 models use fastBPE instead of subword_nmt, see instructions below
#["English-German","English-Russian","English-French","German-English","Russian-English"]
def load_LT_model(language):
    if(language=="English-German"):
        en2de = torch.hub.load('pytorch/fairseq', 'transformer.wmt19.en-de.single_model', tokenizer='moses', bpe='fastbpe')
        output=en2de
    elif(language=="German-English"):
        de2en = torch.hub.load('pytorch/fairseq', 'transformer.wmt19.de-en.single_model', tokenizer='moses', bpe='fastbpe')
        output=de2en
    elif(language=="English-Russian"):
        en2ru = torch.hub.load('pytorch/fairseq', 'transformer.wmt19.en-ru.single_model', tokenizer='moses', bpe='fastbpe')
        output=en2ru
    elif(language=="Russian-English"):
        ru2en = torch.hub.load('pytorch/fairseq', 'transformer.wmt19.ru-en.single_model', tokenizer='moses', bpe='fastbpe')
        output=ru2en
    elif(language=="English-French"):
        en2fr = torch.hub.load('pytorch/fairseq', 'dynamicconv.glu.wmt14.en-fr', tokenizer='moses', bpe='subword_nmt')
        output=en2fr
    else:
        print("No model present to translate")
    return output

@app.route('/translate_language', methods=['POST'])
def translate_language_function():
    try:
        global model_Object_LT
        global model_flag_LT
        #global model_client_src_LT
        global model_client_language
        print(" enter the text that you want to convert")
        #torch.hub.list('pytorch/fairseq')
        print("Torch default path---",torch.hub.get_dir())
        d='/var/www/myWizard.IngrAInAIServices.WebAPI.Python'
        torch.hub.set_dir(d)
        print("Torch updated path---",torch.hub.get_dir())
        req = request.get_json()
        text = req['text']
        #src=req['src']
        #dest = req['dest']
        language=req['language']
        # Preprocessing of the speacial characters in a sentence
        print("Translation language------",language)
        #print("Language of text-------",detect_text)
        print("Text to be translated---",text)
        bad= [';',':', "*","@","#","$","%","^","&","(",")","{","}","/","+","|","_","<","-","=","*"]
        if language in ["English-German","English-Russian","English-French"]:
            #text = re.sub(r'[@_!#$%^&*()<>[\]?/\|\'\"\`}{~:;+-=.,]',r'',text)
            text = ''.join(i for i in text if not i in bad)
            text=re.sub("\s\s+", " ", text)
            unknown_exc=["!!!","!!"]
            unknown_ques=["???","??"]
            unknown_fs=["...",".."]
            unknown_com=[",,,",",,"]
            x=int(len(text)/4)
            for i in range(1,x):
                for word in unknown_exc:
                    text=text.replace(word,"!")
                for word in unknown_ques:
                    text=str(text).replace(word,'?')
                for word in unknown_fs:
                    text=str(text).replace(word,'.')
                for word in unknown_com:
                    text=str(text).replace(word,',')
            print("Text after cleaning spaces and special characters---",text)
            text=text.encode('ascii',"ignore").decode('ascii')
        return_value1 = {}
        return_value1["is_success"] = True
        return_value1["message"] = ""
        return_value1["response"] = ""
        #return_value1["score bleu"] = ""
        # Calling Translation function to translate
        print("length is -",len(str(text)))
        if language not in ["English-German","English-Russian","English-French","German-English","Russian-English"]:
            return_value3 = {}
            return_value3["is_success"] = False
            return_value3["message"] = "Incorrect translation languages given."
            return_value3["response"] = ""
            #return_value3["score bleu"] = ""
            return return_value3
        elif(len(text)<=10):
            return_value3 = {}
            return_value3["is_success"] = False
            return_value3["message"] = "Minimum limit is 11 characters after removing extra spaces and special characters. Please provide meaningful sentence longer than 10 characters."
            return_value3["response"] = ""
            return return_value3
        print("Starting to check global flag logic")
        if not(language==model_client_language):
            model_flag_LT=False
        if model_flag_LT == False:
            model_Object_LT=load_LT_model(language)
            model_flag_LT = True
            model_client_language = language
            #model_client_dest_LT= dest
        print("Global model flag logic implemented")
    
        try:
            if language=="English-Russian":
                try:
                    translation_output=str(TextBlob(text).translate(to='ru'))
                except:
                    translation_output= model_Object_LT.translate(text)
            #print("Model is--",model_Object_LT)
            else:
                translation_output= model_Object_LT.translate(text)
            print("Done")
        except Exception as e:
            print(e)
            return_value2 = {}
            return_value2["is_success"] = False
            return_value2["message"] = "Something went wrong in translation"
            return_value2["response"] = ""
            #score1 = str(sentence_bleu(text,text))
            #score=float(score1[:5])
            return return_value2 
    except Exception as e:
        print(e)
        return_value2 = {}
        return_value2["is_success"] = False
        return_value2["message"] = "Error occured"
        return_value2["response"] = ""
        return return_value2

    return_value1["response"]=translation_output
    return_value1["message"] = "Translation Successful"
    #return_value1["score bleu"]=str(score)
    return jsonify(return_value1)
