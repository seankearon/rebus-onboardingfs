﻿namespace OnboardingMessages

type OnboardNewCustomer =
    {
        Name: string
        Email: string
    }

type CreateCustomerAccount =
    {
        Name: string
        Email: string
    }

type CustomerAccountCreated =
    {
        Email: string
        CustomerId: int
    }

type SendWelcomeEmail =
    {
        CustomerId: int
    }

type ScheduleSalesCall =
    {
        CustomerId: int
    }

type CustomerOnboarded =
    {
        CustomerId: int
    }