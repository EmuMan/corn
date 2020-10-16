def get_base_args(string):
    split = [word for word in string.split(' ') if word != '']
    base = split[0]
    args = [e for e in ' '.join(split[1:]).split(',') if e != '']
    return [base, args]