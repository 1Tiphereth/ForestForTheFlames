def rec(a,b,c):
    total = 0
    print(1)
    while c < 0:
        
        print(1)
        total += a * b
        c -= 1
    return total

print(rec(8,2,8))
print(rec(8,1,8))
print(rec(2,2,8))
print(rec(1,2,8))
