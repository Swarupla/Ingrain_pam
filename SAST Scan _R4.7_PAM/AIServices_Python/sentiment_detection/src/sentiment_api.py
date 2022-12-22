import re
#from nltk.sentiment.vader import SentimentIntensityAnalyzer 
#from keras.preprocessing.text import Tokenizer
#from keras.preprocessing.sequence import pad_sequences
import pickle
from pathlib import Path
#from keras.models import load_model
from shared.logger_helper import GenerateLogger
import numpy as np
import pandas as pd
#log_generator = GenerateLogger()
#currentDirectory = str(Path(__file__).parent.parent.absolute())
#logger = log_generator.generate_logger(currentDirectory + "/sentiment.log")
import platform
import flair
import subprocess
import os
import sys
from flask import Flask, jsonify, request, render_template
from pathlib import Path
import configparser
#from sentiment_analyzer import SentimentAnalyzer
# from sentiment_analyzer import detect_sentiment
import re
from shared.logger_helper import GenerateLogger
log_generator = GenerateLogger()
currentDirectory = str(Path(__file__).parent.parent.absolute())
logger = log_generator.generate_logger(currentDirectory + "/sentiment.log")

app = Flask(__name__)


def load_dict_contractions():
    return {"ain't": 'is not',
 "amn't": 'am not',
 "aren't": 'are not',
 "can't": 'cannot',
 "'cause": 'because',
 "couldn't": 'could not',
 "couldn't've": 'could not have',
 "could've": 'could have',
 "daren't": 'dare not',
 "daresn't": 'dare not',
 "dasn't": 'dare not',
 "didn't": 'did not',
 "doesn't": 'does not',
 "don't": 'do not',
 "e'er": 'ever',
 'em': 'them',
 "everyone's": 'everyone is',
 'finna': 'fixing to',
 'gimme': 'give me',
 'gonna': 'going to',
 "gon't": 'go not',
 'gotta': 'got to',
 "hadn't": 'had not',
 "hasn't": 'has not',
 "haven't": 'have not',
 "he'd": 'he would',
 "he'll": 'he will',
 "he's": 'he is',
 "he've": 'he have',
 "how'd": 'how would',
 "how'll": 'how will',
 "how're": 'how are',
 "how's": 'how is',
 "i'd": 'i would',
 "i'll": 'i will',
 "i'm": 'i am',
 "i'm'a": 'i am about to',
 "i'm'o": 'i am going to',
 "isn't": 'is not',
 "it'd": 'it would',
 "it'll": 'it will',
 "it's": 'it is',
 "i've": 'i have',
 'kinda': 'kind of',
 "let's": 'let us',
 "mayn't": 'may not',
 "may've": 'may have',
 "mightn't": 'might not',
 "might've": 'might have',
 "mustn't": 'must not',
 "mustn't've": 'must not have',
 "must've": 'must have',
 "needn't": 'need not',
 "ne'er": 'never',
 "o'": 'of',
 "o'er": 'over',
 "ol'": 'old',
 "oughtn't": 'ought not',
 "shalln't": 'shall not',
 "shan't": 'shall not',
 "she'd": 'she would',
 "she'll": 'she will',
 "she's": 'she is',
 "shouldn't": 'should not',
 "shouldn't've": 'should not have',
 "should've": 'should have',
 "somebody's": 'somebody is',
 "someone's": 'someone is',
 "something's": 'something is',
 "that'd": 'that would',
 "that'll": 'that will',
 "that're": 'that are',
 "that's": 'that is',
 "there'd": 'there would',
 "there'll": 'there will',
 "there're": 'there are',
 "there's": 'there is',
 "these're": 'these are',
 "they'd": 'they would',
 "they'll": 'they will',
 "they're": 'they are',
 "they've": 'they have',
 "this's": 'this is',
 "those're": 'those are',
 "'tis": 'it is',
 "'twas": 'it was',
 'wanna': 'want to',
 "wasn't": 'was not',
 "we'd": 'we would',
 "we'd've": 'we would have',
 "we'll": 'we will',
 "we're": 'we are',
 "weren't": 'were not',
 "we've": 'we have',
 "what'd": 'what did',
 "what'll": 'what will',
 "what're": 'what are',
 "what's": 'what is',
 "what've": 'what have',
 "when's": 'when is',
 "where'd": 'where did',
 "where're": 'where are',
 "where's": 'where is',
 "where've": 'where have',
 "which's": 'which is',
 "who'd": 'who would',
 "who'd've": 'who would have',
 "who'll": 'who will',
 "who're": 'who are',
 "who's": 'who is',
 "who've": 'who have',
 "why'd": 'why did',
 "why're": 'why are',
 "why's": 'why is',
 "won't": 'will not',
 "wouldn't": 'would not',
 "would've": 'would have',
 "y'all": 'you all',
 "you'd": 'you would',
 "you'll": 'you will',
 "you're": 'you are',
 "you've": 'you have',
 'whatcha': 'what are you',
 'luv': 'love',
 'sux': 'sucks'}


def data_preprocessing(data):
    #Escaping HTML characters
    data = data.lower()
    #data = data.replace('\x92',"'")
    #data = data.replace("’","'")
    data = ' '.join(re.sub(r"(@[A-Za-z0-9]+)|(#[A-Za-z0-9]+)", " ", data).split())
    #Removal of address
    #data = ' '.join(re.sub("(\w+:\/\/\S+)", " ", data).split())
    data=str(data).encode('ascii',"ignore").decode('ascii')
    #Removal of Punctuation
    data = ' '.join(re.sub(r"[$!\.\,\!\?\:\;\-\=\)\(\"]", " ", data).split())
    #Lower case
    #CONTRACTIONS source: https://en.wikipedia.org/wiki/Contraction_%28grammar%29
    CONTRACTIONS = load_dict_contractions()
    words = data.split()
    reformed = [CONTRACTIONS[word] if word in CONTRACTIONS else word for word in words]
    #reformed = [lemmatizer.lemmatize(lancaster.stem(porter.stem(CONTRACTIONS[word]))) if word in CONTRACTIONS else lemmatizer.lemmatize(lancaster.stem(porter.stem(word))) for word in words]
    data = " ".join(reformed)
    data = ' '.join(re.sub("[\']", "", data).split())
    data = " ".join(reformed)
    return data


def detect_sentiment(input_data):
    return_value = {}
    return_value["is_success"] = False
    return_value["message"] = ""
    return_value["response_data"] = []

    try:
        #ss = self._sid.polarity_scores(input)
        #need to add contraction method as well
        #pickle file
        #print('checking new one')
        original_data=input_data
        input_data=data_preprocessing(input_data)
        input_data="\""+str(input_data)+"\""
        dir_path = os.path.dirname(os.path.realpath(__file__))
        python_script=dir_path+'/sentiment_subprocess.py'
        pattern=re.search(r'(.+Python).+',dir_path)
        if platform.system() == 'Windows':
            python_path=pattern.group(1)+'/venv/Scripts/python.exe'
        else:
            if 'app' in dir_path:#pam servers
                python_path='python'
            else:
                python_path=pattern.group(1)+'/IngrAInAIServices_env/bin/python'   
        #cmd="""/var/www/myWizard.IngrAInAIServices.WebAPI.Python/IngrAInAIServices_env/bin/python /var/www/myWizard.IngrAInAIServices.WebAPI.Python/sentiment_detection/src/sentiment_subprocess.py %s"""%input_data
        cmd='{} {} {}'.format(python_path,python_script,input_data)
        #print(cmd)
        out=subprocess.Popen(cmd,stdout=subprocess.PIPE, stderr=subprocess.PIPE,shell=True)
        #print(out.communicate())
        output, err = out.communicate()
        #print('mydata',output)
        out=eval(output.decode().split('\n')[1])
        #{'Output': 'POSITIVE', 'Error': 'No error'}
        if out['Error']=='No error':
            
            data = [{"response": out['Output'] ,"text": str(original_data)}]        
            return_value["is_success"] = True
            return_value["message"] = ""
            return_value["response_data"] = data
        else:
            return_value["message"]=str(out['Error'])
    except Exception as e:
        return_value["message"] = str(e)
    return return_value

# API for detecting tone from a text input
@app.route('/sentiment/detecttone', methods=['POST'])
def detect_sentiment_from_text():
    #analyzer = SentimentAnalyzer()
    return_value = {}
    return_value["is_success"] = False
    return_value["message"] = ""
    return_value["response_data"] = []

    text = ""
    try:
        data = request.get_json()
        text = data["text"]
        text =text.encode('ascii',"ignore").decode('ascii')
        #print("text is---",text)
        string_check= re.compile('[@_!#$%^&* ()<>[\]?/\|\'\"\`}{~:;+-=.,]') 
        flag = 0
        #print("text is---",text)
        for ch in text:
            if(string_check.search(ch) == None):
                flag = 1
        if("text" in data and len(text) > 3 and flag == 1):
            #print("text is---",text)
            #return_value = analyzer.detect_sentiment(text)
            return_value=detect_sentiment(text)
        else:
            return_value["message"] = "Please provide proper text for analyis."
    except Exception as e:
        return_value["message"] = 'System has encountered an error.'
        logger.error(str(e))
    return (return_value,text)


