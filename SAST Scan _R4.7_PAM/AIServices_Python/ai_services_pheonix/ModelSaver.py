from numpy import array
import platform
if platform.system() == 'Linux':
    from keras.preprocessing.text import Tokenizer
    from keras.utils import to_categorical
    from keras.models import Sequential
    from keras.layers import Dense
    from keras.layers import LSTM
    from keras.layers import Embedding
    from keras.preprocessing.sequence import pad_sequences
elif platform.system() == 'Windows':
    from tensorflow.keras.preprocessing.text import Tokenizer
    from tensorflow.keras.utils import to_categorical
    from tensorflow.keras.models import Sequential
    from tensorflow.keras.layers import Dense
    from tensorflow.keras.layers import LSTM
    from tensorflow.keras.layers import Embedding
    from tensorflow.keras.preprocessing.sequence import pad_sequences
import heapq
import numpy
import os,sys

from pathlib import Path
mainDirectory = str(Path(__file__).parent.parent.parent.absolute())
print("Main directory path - "+mainDirectory)
sys.path.insert(0, mainDirectory)
import utils

def load_trained_model(model_name, content,correlationId,uniId):
    try:
        # load model
        loaded_model = utils.load_model(correlationId,model_name,uniId)

                          
        data = content
        # prepare the tokenizer on the source text
        tokenizer = Tokenizer()
        tokenizer.fit_on_texts([data])

        # determine the vocabulary size
        vocab_size = len(tokenizer.word_index) + 1

        # create line-based sequences
        sequences = list()
        for line in data.split('\n'):
            encoded = tokenizer.texts_to_sequences([line])[0]
            for i in range(1, len(encoded)):
                sequence = encoded[:i + 1]
                sequences.append(sequence)

        # pad input sequences
        max_length = max([len(seq) for seq in sequences])
        max_length = max_length - 1

        return loaded_model, tokenizer, max_length

    except Exception as e:
        print(e)
        return None, None, None

def create_model(model_name,content,col,correlationId,uniId):
                
    data = content

    # prepare the tokenizer on the source text
    tokenizer = Tokenizer()
    tokenizer.fit_on_texts([data])

    # determine the vocabulary size
    vocab_size = len(tokenizer.word_index) + 1

    # create line-based sequences
    sequences = list()
    for line in data.split('\n'):
        encoded = tokenizer.texts_to_sequences([line])[0]
        for i in range(1, len(encoded)):
            sequence = encoded[:i + 1]
            sequences.append(sequence)

    # pad input sequences
    max_length = max([len(seq) for seq in sequences])
    sequences = pad_sequences(sequences, maxlen=max_length, padding='pre')

    # split into input and output elements
    sequences = array(sequences)
    X, y = sequences[:,:-1],sequences[:,-1]
    y = to_categorical(y, num_classes=vocab_size)
			
    # define model
    model = Sequential()
    model.add(Embedding(vocab_size, 10, input_length=max_length - 1))
    model.add(LSTM(50))
    model.add(Dense(vocab_size, activation='softmax'))

    # compile network
    model.compile(loss='categorical_crossentropy', optimizer='adam', metrics=['accuracy'])

    # fit network
    model.fit(X, y, epochs=20, verbose=2)
			
    # saving model
    utils.save_model(correlationId,model_name,uniId,model)