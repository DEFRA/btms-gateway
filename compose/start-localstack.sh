#!/bin/bash

ENDPOINT_URL=http://localhost:4966

export AWS_ENDPOINT_URL=$ENDPOINT_URL
export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=local
export AWS_SECRET_ACCESS_KEY=local

declare -a arr=(
        "alvs_decision_fork.fifo"
        "alvs_decision_route.fifo"
        "alvs_error_fork.fifo"
        "alvs_error_route.fifo"
        "customs_clearance_fork.fifo"
        "customs_clearance_route.fifo"
        "customs_error_fork.fifo"
        "customs_error_route.fifo"
        "customs_finalisation_fork.fifo"
        "customs_finalisation_route.fifo"
    )

SNS_ARN=arn:aws:sns:eu-west-2:000000000000
SQS_ARN=arn:aws:sqs:eu-west-2:000000000000

for queueName in "${arr[@]}"
do
   timestamp="$(date +"%T")"
   echo "[$timestamp] Started setting up queue: $queueName"
   aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
   aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
   aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
   echo "[$timestamp] Finished setting up queue: $queueName"
done
