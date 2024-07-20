using Newtonsoft.Json;

namespace StabilityMatrix.Core.Models
{
    public class SafeTensorsInfo
    {
        [JsonProperty("sshs_model_hash")]
        public string SshsModelHash { get; set; }

        [JsonProperty("ss_adaptive_noise_scale")]
        public string SsAdaptiveNoiseScale { get; set; }

        [JsonProperty("ss_num_batches_per_epoch")]
        public string SsNumBatchesPerEpoch { get; set; }

        [JsonProperty("ss_output_name")]
        public string SsOutputName { get; set; }

        [JsonProperty("ss_network_dropout")]
        public string SsNetworkDropout { get; set; }

        [JsonProperty("sshs_legacy_hash")]
        public string SshsLegacyHash { get; set; }

        [JsonProperty("ss_base_model_version")]
        public string SsBaseModelVersion { get; set; }

        [JsonProperty("ss_dataset_dirs")]
        public string SsDatasetDirs { get; set; }

        [JsonProperty("ss_ip_noise_gamma_random_strength")]
        public string SsIpNoiseGammaRandomStrength { get; set; }

        [JsonProperty("ss_huber_schedule")]
        public string SsHuberSchedule { get; set; }

        [JsonProperty("ss_max_grad_norm")]
        public string SsMaxGradNorm { get; set; }

        [JsonProperty("ss_network_dim")]
        public string SsNetworkDim { get; set; }

        [JsonProperty("ss_mixed_precision")]
        public string SsMixedPrecision { get; set; }

        [JsonProperty("modelspec.implementation")]
        public string ModelspecImplementation { get; set; }

        [JsonProperty("ss_sd_model_name")]
        public string SsSdModelName { get; set; }

        [JsonProperty("ss_loss_type")]
        public string SsLossType { get; set; }

        [JsonProperty("ss_flip_aug")]
        public string SsFlipAug { get; set; }

        [JsonProperty("ss_prior_loss_weight")]
        public string SsPriorLossWeight { get; set; }

        [JsonProperty("ss_training_comment")]
        public string SsTrainingComment { get; set; }

        [JsonProperty("modelspec.title")]
        public string ModelspecTitle { get; set; }

        [JsonProperty("ss_scale_weight_norms")]
        public string SsScaleWeightNorms { get; set; }

        [JsonProperty("ss_gradient_checkpointing")]
        public string SsGradientCheckpointing { get; set; }

        [JsonProperty("ss_keep_tokens")]
        public string SsKeepTokens { get; set; }

        [JsonProperty("ss_max_token_length")]
        public string SsMaxTokenLength { get; set; }

        [JsonProperty("ss_color_aug")]
        public string SsColorAug { get; set; }

        [JsonProperty("modelspec.encoder_layer")]
        public string ModelspecEncoderLayer { get; set; }

        [JsonProperty("ss_caption_dropout_rate")]
        public string SsCaptionDropoutRate { get; set; }

        [JsonProperty("modelspec.resolution")]
        public string ModelspecResolution { get; set; }

        [JsonProperty("ss_num_epochs")]
        public string SsNumEpochs { get; set; }

        [JsonProperty("ss_debiased_estimation")]
        public string SsDebiasedEstimation { get; set; }

        [JsonProperty("ss_ip_noise_gamma")]
        public string SsIpNoiseGamma { get; set; }

        [JsonProperty("modelspec.architecture")]
        public string ModelspecArchitecture { get; set; }

        [JsonProperty("modelspec.timestep_range")]
        public string ModelspecTimestepRange { get; set; }

        [JsonProperty("ss_num_train_images")]
        public string SsNumTrainImages { get; set; }

        [JsonProperty("ss_network_module")]
        public string SsNetworkModule { get; set; }

        [JsonProperty("ss_optimizer")]
        public string SsOptimizer { get; set; }

        [JsonProperty("ss_min_snr_gamma")]
        public string SsMinSnrGamma { get; set; }

        [JsonProperty("ss_unet_lr")]
        public string SsUnetLr { get; set; }

        [JsonProperty("ss_noise_offset")]
        public string SsNoiseOffset { get; set; }

        [JsonProperty("ss_cache_latents")]
        public string SsCacheLatents { get; set; }

        [JsonProperty("ss_noise_offset_random_strength")]
        public string SsNoiseOffsetRandomStrength { get; set; }

        [JsonProperty("ss_resolution")]
        public string SsResolution { get; set; }

        [JsonProperty("ss_min_bucket_reso")]
        public string SsMinBucketReso { get; set; }

        [JsonProperty("ss_reg_dataset_dirs")]
        public string SsRegDatasetDirs { get; set; }

        [JsonProperty("ss_lowram")]
        public string SsLowram { get; set; }

        [JsonProperty("ss_tag_frequency")]
        public string SsTagFrequency { get; set; }

        [JsonProperty("ss_caption_tag_dropout_rate")]
        public string SsCaptionTagDropoutRate { get; set; }

        [JsonProperty("ss_sd_model_hash")]
        public string SsSdModelHash { get; set; }

        [JsonProperty("ss_caption_dropout_every_n_epochs")]
        public string SsCaptionDropoutEveryNEpochs { get; set; }

        [JsonProperty("ss_training_finished_at")]
        public string SsTrainingFinishedAt { get; set; }

        [JsonProperty("modelspec.sai_model_spec")]
        public string ModelspecSaiModelSpec { get; set; }

        [JsonProperty("ss_max_bucket_reso")]
        public string SsMaxBucketReso { get; set; }

        [JsonProperty("ss_epoch")]
        public string SsEpoch { get; set; }

        [JsonProperty("ss_full_fp16")]
        public string SsFullFp16 { get; set; }

        [JsonProperty("modelspec.description")]
        public string ModelspecDescription { get; set; }

        [JsonProperty("modelspec.date")]
        public DateTime ModelspecDate { get; set; }

        [JsonProperty("ss_network_alpha")]
        public string SsNetworkAlpha { get; set; }

        [JsonProperty("ss_batch_size_per_device")]
        public string SsBatchSizePerDevice { get; set; }

        [JsonProperty("ss_random_crop")]
        public string SsRandomCrop { get; set; }

        [JsonProperty("modelspec.tags")]
        public string ModelspecTags { get; set; }

        [JsonProperty("ss_v2")]
        public string SsV2 { get; set; }

        [JsonProperty("ss_face_crop_aug_range")]
        public string SsFaceCropAugRange { get; set; }

        [JsonProperty("ss_training_started_at")]
        public string SsTrainingStartedAt { get; set; }

        [JsonProperty("ss_multires_noise_discount")]
        public string SsMultiresNoiseDiscount { get; set; }

        [JsonProperty("ss_total_batch_size")]
        public string SsTotalBatchSize { get; set; }

        [JsonProperty("ss_seed")]
        public string SsSeed { get; set; }

        [JsonProperty("ss_gradient_accumulation_steps")]
        public string SsGradientAccumulationSteps { get; set; }

        [JsonProperty("ss_huber_c")]
        public string SsHuberC { get; set; }

        [JsonProperty("ss_lr_warmup_steps")]
        public string SsLrWarmupSteps { get; set; }

        [JsonProperty("ss_multires_noise_iterations")]
        public string SsMultiresNoiseIterations { get; set; }

        [JsonProperty("ss_bucket_info")]
        public string SsBucketInfo { get; set; }

        [JsonProperty("ss_max_train_steps")]
        public string SsMaxTrainSteps { get; set; }

        [JsonProperty("ss_lr_scheduler")]
        public string SsLrScheduler { get; set; }

        [JsonProperty("ss_zero_terminal_snr")]
        public string SsZeroTerminalSnr { get; set; }

        [JsonProperty("ss_sd_scripts_commit_hash")]
        public string SsSdScriptsCommitHash { get; set; }

        [JsonProperty("ss_learning_rate")]
        public string SsLearningRate { get; set; }

        [JsonProperty("modelspec.author")]
        public string ModelspecAuthor { get; set; }

        [JsonProperty("ss_bucket_no_upscale")]
        public string SsBucketNoUpscale { get; set; }

        [JsonProperty("ss_num_reg_images")]
        public string SsNumRegImages { get; set; }

        [JsonProperty("ss_shuffle_caption")]
        public string SsShuffleCaption { get; set; }

        [JsonProperty("ss_steps")]
        public string SsSteps { get; set; }

        [JsonProperty("ss_clip_skip")]
        public string SsClipSkip { get; set; }

        [JsonProperty("ss_enable_bucket")]
        public string SsEnableBucket { get; set; }

        [JsonProperty("ss_new_sd_model_hash")]
        public string SsNewSdModelHash { get; set; }

        [JsonProperty("modelspec.prediction_type")]
        public string ModelspecPredictionType { get; set; }

        [JsonProperty("ss_text_encoder_lr")]
        public string SsTextEncoderLr { get; set; }

        [JsonProperty("ss_session_id")]
        public string SsSessionId { get; set; }

        public static SafeTensorsInfo? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SafeTensorsInfo>(json);
        }
    }
}
