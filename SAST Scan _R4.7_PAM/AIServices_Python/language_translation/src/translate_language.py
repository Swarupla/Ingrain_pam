#!/usr/bin/env python
# coding: utf-8

# In[1]:


import nltk
import textblob
from textblob import TextBlob
from nltk.translate.bleu_score import sentence_bleu
from langdetect import detect
import re
from flask import Flask, request, jsonify
from flask_cors import CORS
import warnings
import json

app = Flask(__name__)

# In[2]:


# blob = TextBlob("My name is Surajit Majumder")
# print(" translation : ",str(blob.translate(to='es')))


# In[3]:



def user_input(language,text):
    
    if language =='es':
        return(str(TextBlob(text).translate(to='es'))) # spanish translation
            
    elif language =='en':
        return(str(TextBlob(text).translate(to='en'))) # english translation
    elif language == 'zh':
        return(str(TextBlob(text).translate(to='zh'))) # chinese translation
    elif language == 'de':
        return(str(TextBlob(text).translate(to='de'))) # german translation
    elif language == 'da':
        return(str(TextBlob(text).translate(to='da'))) # danish translation
    elif language == 'fr':
        return(str(TextBlob(text).translate(to='fr'))) # french translation
    elif language == 'ar':
        return(str(TextBlob(text).translate(to='ar'))) # arabic translation
    elif language == 'ko':
        return(str(TextBlob(text).translate(to='ko'))) # korean translation
    elif language == 'hi':
        return(str(TextBlob(text).translate(to='hi'))) # korean translation        
        
        
    else:
        print("............error......")


# In[5]:

#@app.route('/tranlatelanguage', methods=['POST'])
def trans_language():
    req = request.get_json()
    text = req['text']
    language = req['language']
    #print(" enter the language you want to convert , it should be like es/en/zh/de/fe")
    #language = str(input())
    #print(" enter the text that you want to convert")
    #text = str(input())
    # Preprocessing of the speacial characters in a sentence
    nstr = re.sub(r'[@_!#$%^&*()<>[\]?/\|\'\"\`}{~:;+-=.,]',r'',text)
    print("..............................................................................")
    return_value1 = {}
    return_value1["is_success"] = True
    return_value1["message"] = ""
    return_value1["response"] = ""
    return_value1["score bleu"] = ""

    # Calling Translation function to translate
    #return_value1["response"] = user_input(language,nstr)
    translation_output= user_input(language,text)
    detect_language=detect(translation_output)
    detect_text=detect(text)    
    print(detect_language)
    print(detect_text)

    if detect_language=='fr':
        translation22=str(TextBlob(translation_output).translate(to='en'))
        score1 = str(sentence_bleu(text,translation22))
        score=float(score1[:5])
        print(score)
    elif detect_language=='de':
        translation22=str(TextBlob(translation_output).translate(to='en'))
        score1 = str(sentence_bleu(text,translation22))
        score=float(score1[:5])
        print(score)
    elif detect_language=='da':
        translation22=str(TextBlob(translation_output).translate(to='en'))
        score1 = str(sentence_bleu(text,translation22))
        score=float(score1[:5])
        print(score)
    elif detect_language=='zh':
        translation22=str(TextBlob(translation_output).translate(to='en'))
        score1 = str(sentence_bleu(text,translation22))
        score=float(score1[:5])
        print(score1)
    elif detect_language=='ar':
        translation22=str(TextBlob(translation_output).translate(to='en'))
        score1 = str(sentence_bleu(text,translation22))
        score=float(score1[:5])
        print(score)
    elif detect_language=='ko':
        translation22=str(TextBlob(translation_output).translate(to='en'))
        score1 = str(sentence_bleu(text,translation22))
        score=float(score1[:5])
        print(score)
    elif detect_language=='es':
        translation22=str(TextBlob(translation_output).translate(to='en'))
        score1 = str(sentence_bleu(text,translation22))
        score=float(score1[:5])
        print(score)
    elif detect_language=='hi':
        translation22=str(TextBlob(translation_output).translate(to='en'))
        score1 = str(sentence_bleu(text,translation22))
        score=float(score1[:5])
        print(score)        


        
    elif detect_language=='en':
        if detect_text=='fr':
            translation22=str(TextBlob(translation_output).translate(to='fr'))
            score1 = str(sentence_bleu(text,translation22))
            score=float(score1[:5])
            print(text)
            print("translation_output",translation_output)
            print(translation22)
            print(score1)
        if detect_text=='de':
            translation22=str(TextBlob(translation_output).translate(to='de'))
            score1 = str(sentence_bleu(text,translation22))
            score=float(score1[:5])
            print(text)
            print("translation_output",translation_output)
            print(translation22)
            print(score)        
        if detect_text=='da':
            translation22=str(TextBlob(translation_output).translate(to='da'))
            score1 = str(sentence_bleu(text,translation22))
            score=float(score1[:5])
            print(text)
            print("translation_output",translation_output)
            print(translation22)
            print(score)
            
        if detect_text=='zh':
            translation22=str(TextBlob(translation_output).translate(to='zh'))
            score1 = str(sentence_bleu(text,translation22))
            score=float(score1[:5])
            print(text)
            print("translation_output",translation_output)
            print(translation22)
            print(score)       
        if detect_text=='ar':
            translation22=str(TextBlob(translation_output).translate(to='ar'))
            score1 = str(sentence_bleu(text,translation22))
            score=float(score1[:5])
            print(text)
            print("translation_output",translation_output)
            print(translation22)
            print(score)       
            
        if detect_text=='ko':
            translation22=str(TextBlob(translation_output).translate(to='ko'))
            score1 = str(sentence_bleu(text,translation22))
            score=float(score1[:5])
            print(text)
            print("translation_output",translation_output)
            print(translation22)
            print(score)        
        if detect_text=='es':
            translation22=str(TextBlob(translation_output).translate(to='es'))
            score1 = str(sentence_bleu(text,translation22))
            score=float(score1[:5])
            print(text)
            print("translation_output",translation_output)
            print(translation22)
            print(score)      
        if detect_text=='hi':
            translation22=str(TextBlob(translation_output).translate(to='hi'))
            score1 = str(sentence_bleu(text,translation22))
            score=float(score1[:5])
            print(text)
            print("translation_output",translation_output)
            print(translation22)
            print(score)                  
    else :
        print("Invalid language")
    
    #translation_english=str(TextBlob(translation_22).translate(to='en'))
    #score1 = sentence_bleu(translation_english,text)
    return_value1["response"]=translation_output
    return_value1["score bleu"]=str(score)
    return jsonify(return_value1)
    #print(" Language Translation : " , translation_output )


if __name__ == "__main__":
    app.run()



