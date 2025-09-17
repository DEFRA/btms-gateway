#!/bin/bash

ENDPOINT_URL=http://sqs.eu-west-2.localhost.localstack.cloud:4566

export AWS_ENDPOINT_URL=$ENDPOINT_URL
export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=local
export AWS_SECRET_ACCESS_KEY=local

# SNS/SQS topics, queues, subscriptions

ICDR_Topic=trade_imports_inbound_customs_declarations.fifo
ICDR_Queue=trade_imports_inbound_customs_declarations_processor.fifo
OCD_Queue=trade_imports_data_upserted_btms_gateway
OCD_DeadLetterQueue=trade_imports_data_upserted_btms_gateway-deadletter

# Create Topics
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $ICDR_Topic

# Create Queues
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $ICDR_Queue
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --queue-name $OCD_Queue
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --queue-name $OCD_DeadLetterQueue

SNS_ARN=arn:aws:sns:eu-west-2:000000000000
SQS_ARN=arn:aws:sqs:eu-west-2:000000000000

# Create Subscriptions
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$ICDR_Topic --protocol sqs --notification-endpoint $SQS_ARN:$ICDR_Queue --attributes '{"RawMessageDelivery": "true"}'

# Create Redrive Policy
aws --endpoint-url=$ENDPOINT_URL sqs set-queue-attributes --queue-url $ENDPOINT_URL/000000000000/$OCD_Queue --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-west-2:000000000000:trade_imports_data_upserted_btms_gateway-deadletter\",\"maxReceiveCount\":\"1\"}"}'

function is_ready() {
    aws --endpoint-url=http://sqs.eu-west-2.localhost.localstack.cloud:4566 sns list-topics --query "Topics[?ends_with(TopicArn, ':trade_imports_inbound_customs_declarations.fifo')].TopicArn" || return 1
    
    aws --endpoint-url=http://sqs.eu-west-2.localhost.localstack.cloud:4566 sqs get-queue-url --queue-name trade_imports_inbound_customs_declarations_processor.fifo || return 1
    aws --endpoint-url=http://sqs.eu-west-2.localhost.localstack.cloud:4566 sqs get-queue-url --queue-name trade_imports_data_upserted_btms_gateway || return 1
    aws --endpoint-url=http://sqs.eu-west-2.localhost.localstack.cloud:4566 sqs get-queue-url --queue-name trade_imports_data_upserted_btms_gateway-deadletter || return 1
    return 0
}

while ! is_ready; do
    echo "Waiting until ready"
    sleep 1
done

touch /tmp/ready