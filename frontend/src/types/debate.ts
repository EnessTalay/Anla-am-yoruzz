export interface StartDebateRequest {
  topic: string;
  person1Name: string;
  person1View: string;
  person2Name: string;
  person2View: string;
}

export interface AnswerItemDto {
  questionId: string;
  answerText: string;
}

export interface AnalyzeDebateRequest {
  debateId: string;
  answers: AnswerItemDto[];
}

export interface QuestionDto {
  questionId: string;
  questionText: string;
  forSide: string;
}

export interface StartDebateResponse {
  debateId: string;
  questions: QuestionDto[];
}

export interface VennDto {
  leftPoints: string[];
  rightPoints: string[];
  bothPoints: string[];
}

export interface ScoreDto {
  leftScore: number;
  rightScore: number;
  description: string;
}

export interface EmotionDto {
  side: string;
  emotion: string;
  intensity: number;
}

export interface ActionStepDto {
  id: string;
  stepText: string;
  isCompleted: boolean;
}

export interface AnalysisResultDto {
  debateId: string;
  status: string;
  venn: VennDto;
  scores: ScoreDto;
  emotions: EmotionDto[];
  verdict: string;
  suggestion: string;
  actionSteps: ActionStepDto[];
}
